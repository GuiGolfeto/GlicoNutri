# GlicoNutri — aplicativo do paciente

Flutter, para Android e iOS. Consome a mesma API da web; nunca fala com o banco.

Instalação no Windows e roteiro completo de testes: `INSTALACAO-E-TESTES.md`.

## Rodar

```bash
./dev.sh up                                # sobe a API, na raiz do repositório
cd mobile
flutter run                                # simulador iOS ou emulador Android
flutter test                               # testes
```

Fora de desenvolvimento, o endereço da API é obrigatório e precisa ser HTTPS (RN31):

```bash
flutter build apk --dart-define=API_URL=https://.../api
```

## Duas armadilhas de ambiente

- **Projeto dentro de `~/Desktop`**: a pasta é sincronizada e acrescenta atributos que
  fazem o `codesign` recusar o Flutter.framework ("resource fork, Finder information,
  or similar detritus not allowed"). Aponte a pasta de build para fora:
  `rm -rf build && mkdir -p ~/Library/Caches/gliconutri-build && ln -s ~/Library/Caches/gliconutri-build build`
- **`./dev.sh up --local`** usa a porta 5433, que pode estar ocupada por outro projeto. A
  API então conecta no banco errado. Confira com `docker ps` antes.

## Lembretes

Não há push: as notificações são locais, agendadas no aparelho. O app não executa quando
o aviso toca, então `lib/alertas/ciclo_alertas.dart` guarda o que programou e informa o
desfecho à API (RN27) quando o paciente abre o app. Consequências:

- A API só fica sabendo de um disparo na próxima abertura do app.
- A agenda cobre 7 dias; sem abrir o app por mais tempo que isso, os avisos acabam.
- O alerta de glicemia repete a cada 30 minutos por até 2 horas (RN26 e RN35).

A fonte Inter vem do Google Fonts, sob a licença SIL Open Font License.
