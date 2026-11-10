# Ajustes nos casos de uso para alinhar ao DER

Decisão do Mateus em 10/11/2026: onde o caso de uso pede um campo que o DER
V2.0 não tem, **vale o DER**. O sistema já segue essa regra; abaixo está o que
mudar em cada caso de uso para o texto bater com o banco.

| Caso de uso | Trecho atual | O que mudar |
|---|---|---|
| **UC002 – Cadastrar Paciente**, passo 5 | "O ator preenche o contato e endereço." | Trocar por "O ator preenche o telefone de contato." O DER não tem endereço para o paciente |
| **UC003 – Cadastrar Nutricionista**, passo 2 | "Nome completo, CPF, Data de Nascimento, Sexo." | Trocar por "Nome completo, CRN, especialidade e telefone." O DER não guarda CPF, nascimento nem sexo do nutricionista |
| **UC003**, fluxo A1 – Validar CPF | Validação do dígito verificador do CPF | Remover o fluxo A1, porque o nutricionista não tem CPF no DER. A validação passa a ser a unicidade do CRN |
| **UC008 – Registrar Dados Antropométricos**, passo 7 | "O nutricionista adiciona observações clínicas opcionais." | Remover o passo. O DER não tem campo de observação no registro antropométrico |

Antes de editar, vale conferir no DER se algum desses campos foi esquecido na
modelagem e devia existir. Parte da documentação saiu com apoio de IA. Se o
campo tiver que existir, o caminho é o contrário: entra no DER e no sistema.
