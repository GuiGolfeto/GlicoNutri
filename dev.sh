#!/usr/bin/env bash
# Sobe, derruba e inspeciona o ambiente de desenvolvimento do GlicoNutri.
#
#   ./dev.sh up        sobe banco, API e front
#   ./dev.sh up --supabase   idem, mas a API aponta para o Supabase
#   ./dev.sh down      derruba API e front (o banco continua de pé)
#   ./dev.sh status    mostra o que está rodando
#   ./dev.sh logs api|web    acompanha o log
#   ./dev.sh test      roda a suite de testes

set -euo pipefail
cd "$(dirname "$0")"

RAIZ="$(pwd)"
LOGS="$RAIZ/.logs"
API_LOG="$LOGS/api.log"
WEB_LOG="$LOGS/web.log"

mkdir -p "$LOGS"

verde()   { printf "\033[32m%s\033[0m\n" "$1"; }
amarelo() { printf "\033[33m%s\033[0m\n" "$1"; }
vermelho(){ printf "\033[31m%s\033[0m\n" "$1"; }

# O dotnet instalado pelo cask nem sempre esta no PATH de shells nao interativos.
export PATH="$PATH:/usr/local/share/dotnet:$HOME/.dotnet/tools"

esperar() {   # esperar <url> <segundos> <rotulo>
  local url="$1" limite="$2" rotulo="$3" i=0
  while [ "$i" -lt "$limite" ]; do
    if curl -sf -o /dev/null "$url" 2>/dev/null || curl -s -o /dev/null "$url" 2>/dev/null; then
      verde "  $rotulo no ar"
      return 0
    fi
    sleep 1
    i=$((i + 1))
  done
  vermelho "  $rotulo nao respondeu em ${limite}s — veja $LOGS"
  return 1
}

subir_banco() {
  if ! docker info >/dev/null 2>&1; then
    amarelo "  Docker parado; iniciando OrbStack…"
    open -a OrbStack 2>/dev/null || open -a Docker 2>/dev/null || {
      vermelho "  Nenhum runtime de container encontrado."; exit 1; }
    local i=0
    while ! docker info >/dev/null 2>&1 && [ "$i" -lt 60 ]; do sleep 1; i=$((i + 1)); done
  fi

  docker compose up -d >/dev/null 2>&1
  local i=0
  while [ "$i" -lt 60 ]; do
    [ "$(docker inspect --format='{{.State.Health.Status}}' gliconutri-db 2>/dev/null)" = "healthy" ] && break
    sleep 1; i=$((i + 1))
  done
  verde "  Postgres no ar (localhost:5433)"
}

subir_api() {
  local usar_supabase="${1:-false}"

  ( cd backend/GlicoNutri.Api
    if [ "$usar_supabase" = "true" ]; then
      amarelo "  API apontando para o Supabase"
      USAR_SUPABASE=true ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5080 \
        nohup dotnet run --no-launch-profile > "$API_LOG" 2>&1 &
    else
      # Garante o schema no banco local antes de subir.
      dotnet ef database update >/dev/null 2>&1 || true
      ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://localhost:5080 \
        nohup dotnet run --no-launch-profile > "$API_LOG" 2>&1 &
    fi )

  esperar "http://localhost:5080/api/auth/eu" 90 "API (localhost:5080)"
}

subir_web() {
  ( cd web
    [ -d node_modules ] || { amarelo "  instalando dependencias do front…"; npm install --silent; }
    nohup npm run dev > "$WEB_LOG" 2>&1 & )

  esperar "http://localhost:5173" 60 "Front (localhost:5173)"
}

case "${1:-up}" in
  up)
    supabase=false
    [ "${2:-}" = "--supabase" ] && supabase=true

    echo "Subindo o GlicoNutri…"
    subir_banco
    pkill -f "GlicoNutri.Api" 2>/dev/null || true
    pkill -f "vite" 2>/dev/null || true
    sleep 1
    subir_api "$supabase"
    subir_web

    echo
    verde "Pronto."
    echo "  Front .... http://localhost:5173"
    echo "  API ...... http://localhost:5080"
    echo "  OpenAPI .. http://localhost:5080/openapi/v1.json"
    echo
    echo "  Login inicial: admin@gliconutri.local / GlicoNutri@2026"
    echo "  Senhas provisorias dos cadastros aparecem em: ./dev.sh logs api"
    ;;

  down)
    pkill -f "GlicoNutri.Api" 2>/dev/null && verde "  API parada" || amarelo "  API ja estava parada"
    pkill -f "vite" 2>/dev/null && verde "  Front parado" || amarelo "  Front ja estava parado"
    amarelo "  O Postgres continua de pe. Para derrubar: docker compose down"
    ;;

  status)
    pgrep -f "GlicoNutri.Api" >/dev/null && verde "  API ........ rodando" || vermelho "  API ........ parada"
    pgrep -f "vite" >/dev/null && verde "  Front ...... rodando" || vermelho "  Front ...... parado"
    docker ps --format '{{.Names}}' 2>/dev/null | grep -q gliconutri-db \
      && verde "  Postgres ... rodando" || vermelho "  Postgres ... parado"
    ;;

  logs)
    case "${2:-api}" in
      api) tail -f "$API_LOG" ;;
      web) tail -f "$WEB_LOG" ;;
      *)   vermelho "  use: ./dev.sh logs api|web" ;;
    esac
    ;;

  test)
    subir_banco
    dotnet test backend/GlicoNutri.Tests --nologo
    ;;

  *)
    echo "uso: ./dev.sh [up [--supabase] | down | status | logs api|web | test]"
    exit 1
    ;;
esac
