# Segurança — resumo para a banca

## As camadas de defesa, de fora para dentro

1. **Conexão com o banco**: só a API conecta no Supabase; o endpoint REST
   público dele está fechado (RLS ligado, sem política para `anon`).
2. **Senha**: hash com bcrypt, nunca em claro. Bloqueio progressivo depois de
   tentativas inválidas (5 min, depois 30 min, depois 24h).
3. **Acesso**: token expira em 24h, e cada nutricionista só alcança os
   próprios pacientes — mesmo sabendo o ID de outro, a API recusa.
4. **Dado em repouso**: o disco do Supabase já cifra tudo, nível de infra
   (AES-256). Isso protege contra quem rouba o disco.
5. **Dado sensível, cifrado pela aplicação**: CPF, telefone, observações
   clínicas e medicação em uso têm uma segunda cifra, feita pelo próprio
   sistema (AES-256-GCM) — protege mesmo quem lesse a tabela direto, sem
   passar pela API.

## A escolha de não cifrar tudo

Cifrar glicemia, plano alimentar e os agregados do dashboard quebraria busca,
ordenação e os gráficos — e não cifrar esses dados não é uma falha, é um
custo/benefício: o retorno clínico (que já é de quem já olha pela API, atrás
de login) não compensa o que se perde em performance. Segurança é estar
atento até onde o dado exige, não maximizar criptografia a qualquer custo.

## O detalhe técnico que vale mencionar se perguntarem

O CPF é único e é buscado no cadastro. Cifra com texto aleatório (a prática
recomendada) impede comparar duas cifras do mesmo CPF — então a unicidade e a
busca não são feitas pelo CPF cifrado, e sim por um hash determinístico dele,
guardado à parte. É a mesma ideia de guardar hash de senha: não reversível,
mas comparável.
