# Resumo das Configurações de Portas - Metacore Stack

## Objetivo

Analisar e ajustar as configurações de endereço e porta do RoomServer, RoomOperator, client de testes e API de integração do Dashboard para garantir que todos estejam configurados corretamente e prontos para serem executados.

## Problemas Identificados

### Antes das Alterações

1. **RoomServer**: Não tinha configuração explícita de porta, usava porta padrão 5000
2. **RoomOperator**: Configurado para porta 8080, mas o Dashboard esperava porta 40802
3. **Test Client**: Configurado para portas 5000/8080, mas o Dashboard esperava 40801/40802
4. **Dashboard**: Já configurado corretamente para portas 40801/40802/40901

### Inconsistências

- O Dashboard esperava RoomServer em 40801, mas ele rodava em 5000
- O Dashboard esperava RoomOperator em 40802, mas ele rodava em 8080
- Test client estava desalinhado com as expectativas do Dashboard

## Solução Implementada

Padronizamos todas as portas baseadas nas configurações de teste existentes (appsettings.Test.json) e nas expectativas do Dashboard:

### Portas Padronizadas

| Componente       | Porta | URL                        |
| ---------------- | ----- | -------------------------- |
| RoomServer       | 40801 | http://localhost:40801     |
| RoomOperator     | 40802 | http://localhost:40802     |
| Integration API  | 40901 | http://localhost:40901     |
| Dashboard UI     | 5173  | http://localhost:5173      |

## Arquivos Modificados

### Configurações (.json)

1. **server-dotnet/src/RoomServer/appsettings.json**
   - Adicionado: Configuração Kestrel para porta 40801
   ```json
   "Kestrel": {
     "Endpoints": {
       "Http": {
         "Url": "http://localhost:40801"
       }
     }
   }
   ```

2. **server-dotnet/operator/appsettings.json**
   - Atualizado: `RoomServer.BaseUrl` de 5000 para 40801
   - Atualizado: `HttpApi.Port` de 8080 para 40802

3. **server-dotnet/operator/test-client/config.js**
   - Atualizado: `operator.baseUrl` para http://localhost:40802
   - Atualizado: `roomServer.baseUrl` para http://localhost:40801

### Scripts (.sh)

1. **server-dotnet/operator/scripts/run-integration-test.sh**
   - Atualizado: Verificações de porta para 40801/40802
   - Atualizado: Variáveis de ambiente padrão

2. **server-dotnet/operator/scripts/run-tests.sh**
   - Atualizado: Variáveis de ambiente padrão

3. **server-dotnet/operator/scripts/run-operator.sh**
   - Atualizado: Referências de porta e mensagens

### Documentação (.md)

1. **server-dotnet/operator/README.md**
   - Atualizado: Porta padrão do operador

2. **server-dotnet/operator/test-client/README.md**
   - Atualizado: Todas as referências de porta
   - Atualizado: Exemplos de configuração

3. **server-dotnet/operator/test-client/index.js**
   - Atualizado: Texto de ajuda com portas corretas

4. **README.md** (raiz)
   - Adicionado: Seção de configuração de portas
   - Adicionado: Link para PORT_CONFIGURATION.md

5. **PORT_CONFIGURATION.md** (novo)
   - Guia completo de configuração de portas
   - Exemplos de uso
   - Troubleshooting

## Testes Realizados

### Validação de Build
✅ Todos os projetos .NET compilam sem erros
```bash
cd server-dotnet && dotnet build -c Debug
# Build succeeded
```

### Validação de Configuração JSON
✅ Todos os arquivos JSON são válidos
- server-dotnet/src/RoomServer/appsettings.json
- server-dotnet/operator/appsettings.json
- configs/dashboard.settings.json

### Testes de Inicialização

✅ **RoomServer** inicia na porta correta:
```
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:40801
```

✅ **RoomOperator** inicia na porta correta e conecta ao RoomServer:
```
info: Program[0]
      Connecting to RoomServer at http://localhost:40801
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://0.0.0.0:40802
```

## Como Executar

### 1. Iniciar RoomServer
```bash
cd server-dotnet/src/RoomServer
dotnet run
# Escutando em: http://localhost:40801
```

### 2. Iniciar RoomOperator
```bash
cd server-dotnet/operator
dotnet run
# Escutando em: http://localhost:40802
# Conectando ao RoomServer em: http://localhost:40801
```

### 3. Iniciar Integration API (opcional, para Dashboard)
```bash
cd tools/integration-api
npm install
npm run dev
# API escutando na porta 40901
```

### 4. Iniciar Dashboard (opcional)
```bash
cd apps/operator-dashboard
npm install
npm run dev
# Dashboard em: http://localhost:5173
```

### 5. Executar Testes
```bash
cd server-dotnet/operator/test-client
npm install
npm run test:all
# Usa OPERATOR_URL=http://localhost:40802
# Usa ROOMSERVER_URL=http://localhost:40801
```

## Variáveis de Ambiente

Você pode sobrescrever as portas padrão usando variáveis de ambiente:

```bash
# Para o RoomOperator
export OPERATOR_URL=http://localhost:40802

# Para o RoomServer
export ROOMSERVER_URL=http://localhost:40801

# Para autenticação (opcional)
export ROOM_AUTH_TOKEN=seu-token-aqui
```

## Scripts de Integração

Todos os scripts de teste de integração foram atualizados:

```bash
# Teste de integração completo
./server-dotnet/operator/scripts/run-integration-test.sh

# Executar testes manualmente
./server-dotnet/operator/scripts/run-tests.sh
```

## Benefícios

1. **Consistência**: Todas as configurações agora estão alinhadas
2. **Documentação**: Guia completo de portas disponível
3. **Compatibilidade**: Funciona com Dashboard e todos os testes existentes
4. **Manutenibilidade**: Fácil identificar e modificar portas no futuro

## Migração

### Portas Alteradas

- **RoomServer**: 5000 → 40801
- **RoomOperator**: 8080 → 40802

### Por que estas portas?

1. Já estavam configuradas em appsettings.Test.json
2. Alinhadas com as expectativas do Dashboard
3. Evitam conflitos com portas comuns de desenvolvimento
4. Faixa de portas altas que não requer privilégios de admin

## Troubleshooting

### Porta já em uso

```bash
# Verificar o que está usando a porta
lsof -ti:40801
lsof -ti:40802

# Matar o processo
kill $(lsof -t -i:40801)
kill $(lsof -t -i:40802)
```

### RoomOperator não conecta ao RoomServer

1. Verificar se RoomServer está rodando: `curl http://localhost:40801/health`
2. Verificar logs para erros de conexão
3. Verificar firewall não está bloqueando a porta

## Próximos Passos

✅ Configurações atualizadas e testadas
✅ Documentação criada
✅ Build validado
✅ Testes de inicialização realizados

O repositório está agora configurado e pronto para ser executado com as portas padronizadas!

## Referências

- [PORT_CONFIGURATION.md](PORT_CONFIGURATION.md) - Guia completo em inglês
- [QUICKSTART.md](QUICKSTART.md) - Guia rápido do Dashboard
- [DASHBOARD_README.md](DASHBOARD_README.md) - Documentação completa do Dashboard
