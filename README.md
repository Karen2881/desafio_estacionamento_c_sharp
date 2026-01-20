# Sistema de Estacionamento (Frontend + Backend)

Aplicação simples de exemplo composta por:

- Backend: ASP.NET Core (API REST) + SQLite — endpoints para cadastrar, listar e finalizar veículo (calculo de cobrança conforme tabela de preços com vigência).
- Frontend: HTML + JavaScript (index.html + script.js) — interface mínima para testar cadastro, listagem e remoção.

Objetivo: demonstrar CRUD, persistência local e regras de negócio (meia hora, hora inicial, horas adicionais com tolerância de 10 minutos).

Tecnologias
- .NET 8 (ASP.NET Core)
- SQLite (arquivo local)
- HTML, Vanilla JavaScript, CSS

Estrutura sugerida do repositório
- /backend  — código da API (Projeto ASP.NET Core)
- /frontend — index.html, script.js, style.css
- README.md

Pré-requisitos
- .NET SDK 8 instalado
- Navegador moderno (Chrome/Edge/Firefox)
- Opcional: Node.js ou Python para servir arquivos estáticos localmente

Como executar

Backend (API)
1. Abra a solução/projeto no Visual Studio ou terminal.
2. Ou, no terminal, dentro da pasta do backend: dotnet run
3. Verifique saída do servidor — deve indicar algo como: Now listening on: http://localhost:5094

Se a porta for diferente, atualize a constante `API_URL` em `frontend/script.js`.

Observações:
- A inicialização do banco é feita por `DatabaseInitializer`. O arquivo SQLite ficará em `bin/.../database/estacionamento.db` (o caminho é impresso no console).
- CORS: para facilitar testes locais, o backend já habilita política `AllowAll`. Em produção restrinja as origens.

Frontend
- Opção rápida: abra `frontend/index.html` diretamente no navegador - Dentro da pasta do frontend, dê um duplo clique em `index.html`.

Endpoints da API
- POST /api/veiculos
- Corpo JSON: `{ "placa": "ABC-1234" }`
- Respostas: 200 OK (sucesso) ou 409 Conflict (placa duplicada)
- GET /api/veiculos
- Retorna lista de veículos com DataEntrada.
- DELETE /api/veiculos/{placa}?horas={H}&minutos={M}&modo={duracao|absoluto}
- Calcula cobrança e remove o veículo. Retorna JSON com Placa, DataEntrada, DataSaida, Minutos e Valor.

- Parâmetros:
 - modo=duracao (padrão): horas/minutos são considerados duração adicionada à DataEntrada.
 - modo=absoluto: horas/minutos são interpretados como hora do dia (ex.: 21,30 => 21:30; se antes da entrada, assume dia seguinte).