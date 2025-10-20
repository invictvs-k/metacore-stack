# Verificação do Ambiente e Dependências

Este documento descreve o processo de verificação completa do ambiente de desenvolvimento do Metacore Stack.

## Visão Geral

O script de verificação `verify-environment` valida automaticamente:
- Versões das ferramentas de desenvolvimento
- Instalação de dependências
- Construção dos projetos
- Configuração de serviços externos

## Uso Rápido

### Linux/macOS
```bash
# Usando Make
make verify-environment

# Ou diretamente
./tools/scripts/verify-environment.sh
```

### Windows
```powershell
# Usando PowerShell
.\tools\scripts\verify-environment.ps1
```

## O Que é Verificado

### 1. Versões de Ferramentas

O script verifica as versões das seguintes ferramentas e as compara com as versões esperadas:

| Ferramenta | Versão Esperada | Observações |
|------------|----------------|-------------|
| Node.js    | v20.x          | Obrigatório |
| pnpm       | v9.x           | Instalado automaticamente se ausente |
| .NET SDK   | v8.x ou superior | Obrigatório |
| Docker     | Qualquer       | Opcional (para infra local) |

**Ação:** O script registra a versão atual e reporta incompatibilidades.

### 2. Execução do Bootstrap

O script executa `make bootstrap` e monitora:
- Instalação do pnpm (se necessário)
- Instalação das dependências do `schemas`
- Instalação das dependências do `mcp-ts`
- Restauração das dependências do `server-dotnet`

**Ação:** Captura e reporta quaisquer erros ou warnings durante o processo.

### 3. Instalações Específicas

Valida as instalações individuais de cada componente:

```bash
pnpm -C mcp-ts install     # Dependências MCP TypeScript
pnpm -C ui install         # Dependências da UI
dotnet restore server-dotnet  # Dependências .NET
```

**Ação:** Confirma sucesso de cada instalação e identifica peer dependency warnings.

### 4. Testes de Build

Executa builds dos principais componentes para validar a compilação:

```bash
dotnet build server-dotnet/src/RoomServer/RoomServer.csproj -c Debug
pnpm -C mcp-ts build
```

**Ação:** Verifica se os projetos compilam sem erros críticos.

### 5. Verificação de Conectividade Externa

Se configurado, valida:
- Presença de `docker-compose.yml`
- Validação da configuração do docker-compose
- Instruções para iniciar serviços externos

**Ação:** Fornece comandos para iniciar e verificar serviços externos.

## Saída do Script

### Console

O script fornece feedback visual em tempo real:
- ✓ (Verde) - Sucesso
- ⚠ (Amarelo) - Avisos
- ✗ (Vermelho) - Erros
- ℹ (Azul) - Informações

### Relatório em Markdown

Um relatório detalhado é gerado com o nome:
```
environment-validation-report-YYYYMMDD-HHMMSS.md
```

Este relatório inclui:
- Todas as verificações realizadas
- Versões completas das ferramentas
- Logs de warnings e erros
- Instruções para inicialização manual de serviços
- Resumo final com contadores de erros e avisos

### Logs Detalhados

Logs individuais são salvos em:
- Linux/macOS: `/tmp/metacore-verification/`
- Windows: `%TEMP%\metacore-verification\`

Incluindo:
- `bootstrap.log` - Log completo do bootstrap
- `mcp-install.log` - Log da instalação do mcp-ts
- `ui-install.log` - Log da instalação da ui
- `dotnet-restore.log` - Log da restauração .NET
- `dotnet-build.log` - Log do build .NET
- `mcp-build.log` - Log do build TypeScript
- `docker-compose.log` - Log da validação docker-compose

## Inicialização de Serviços (Manual)

Após a verificação bem-sucedida, inicie os serviços manualmente:

### Terminal 1 - MCP TypeScript Services
```bash
pnpm -C mcp-ts dev
```

**Monitore por:**
- Mensagens de erro na inicialização
- Avisos de dependências faltantes
- Confirmação de que os serviços estão rodando
- Portas em uso

### Terminal 2 - Room Server (.NET)
```bash
make run-server
# ou
dotnet run --project server-dotnet/src/RoomServer/RoomServer.csproj
```

**Monitore por:**
- Erros de configuração
- Avisos de conexão com serviços externos
- Confirmação de inicialização do SignalR
- Porta HTTP/HTTPS em uso

## Serviços Externos (Opcional)

Se configurado via docker-compose:

```bash
cd infra
docker compose up -d
```

Verifique status:
```bash
docker compose ps
```

## Interpretação dos Resultados

### ✓ Todos os checks passaram
O ambiente está pronto para desenvolvimento. Você pode prosseguir com:
```bash
make run-server  # Em um terminal
pnpm -C mcp-ts dev  # Em outro terminal
```

### ⚠ Completado com avisos
O ambiente está funcional, mas há avisos:
- **Versão de ferramenta diferente:** Geralmente aceitável se maior que a esperada
- **Peer dependency warnings:** Comuns em projetos Node.js, geralmente não bloqueiam
- **NuGet warnings:** Pacotes resolvidos com versões diferentes, geralmente compatíveis

**Ação recomendada:** Revise os avisos no relatório. Se forem apenas sobre versões mais novas, prossiga.

### ✗ Falhou
Há erros críticos que impedem o desenvolvimento:
- **Ferramenta faltando:** Instale a ferramenta indicada
- **Build failure:** Revise os logs detalhados e corrija erros de código/configuração
- **Dependency errors:** Verifique conectividade de rede e disponibilidade de registros

**Ação recomendada:** Corrija os erros reportados e execute o script novamente.

## Troubleshooting

### pnpm não encontrado após bootstrap
```bash
# Linux/macOS
export PATH="$HOME/.local/share/pnpm:$PATH"

# Ou reinstale globalmente
npm install -g pnpm@9
```

### .NET SDK não encontrado
Instale o .NET 8 SDK de: https://dotnet.microsoft.com/download

### Erros de permissão (Linux/macOS)
```bash
chmod +x tools/scripts/verify-environment.sh
```

### Docker não disponível
Docker é opcional. Se não precisar de serviços externos (banco de dados, etc.), ignore este aviso.

### Build failures
1. Revise os logs em `/tmp/metacore-verification/` (ou `%TEMP%\metacore-verification\`)
2. Verifique conectividade de rede para download de dependências
3. Execute manualmente os comandos que falharam para mais detalhes

## Integração CI/CD

Este script pode ser integrado em pipelines CI/CD:

```yaml
# Exemplo GitHub Actions
- name: Verify Environment
  run: |
    chmod +x tools/scripts/verify-environment.sh
    ./tools/scripts/verify-environment.sh
    
- name: Upload Verification Report
  if: always()
  uses: actions/upload-artifact@v3
  with:
    name: environment-report
    path: environment-validation-report-*.md
```

## Versões Suportadas

De acordo com a documentação do projeto:
- **Node.js:** v20.x
- **pnpm:** v9.x
- **.NET SDK:** v8.x ou superior
- **Docker:** Qualquer versão recente (opcional)

## Quando Executar

Execute o script de verificação:
- ✓ Ao configurar um novo ambiente de desenvolvimento
- ✓ Após atualizar ferramentas do sistema
- ✓ Quando encontrar problemas de build ou execução
- ✓ Antes de reportar bugs (inclua o relatório)
- ✓ Em pipelines CI/CD para validar ambiente de build

## Suporte

Se o script reportar erros que você não consegue resolver:
1. Revise o relatório gerado (`environment-validation-report-*.md`)
2. Verifique os logs detalhados na pasta de logs
3. Consulte a seção de Troubleshooting deste documento
4. Ao reportar issues, inclua o relatório completo
