# Estratégia de sincronização de símbolos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Sincronização de Símbolos** replica o utilitário MetaTrader `SymbolSyncEA` dentro do ambiente StockSharp. A estratégia mantém sincronizados o símbolo da estratégia principal e todas as estratégias vinculadas cadastradas. Sempre que o símbolo primário muda, a estratégia propaga automaticamente a nova segurança para cada estratégia vinculada, garantindo que todo o espaço de trabalho siga o mesmo instrumento sem intervenção manual.

## Ideias centrais
- Capture a segurança da estratégia inicial na inicialização e reutilize-a como uma opção alternativa.
- Mantenha uma lista configurável de estratégias vinculadas que sempre devem espelhar a segurança principal.
- Permitir alterações de símbolos acionadas por uma atribuição `Security` direta ou pela especificação de um novo identificador de segurança.
- Fornece sincronização manual e operações de redefinição para corresponder ao comportamento original do Expert Advisor.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `ChartLimit` | Número máximo de estratégias vinculadas que podem ser sincronizadas. Evita atualizações em massa acidentais. | `10` |
| `SyncSecurityId` | Identificador do título propagado para estratégias vinculadas. Um valor vazio retorna para a segurança da estratégia. | `""` |

## Métodos públicos
- `RegisterLinkedStrategy(Strategy strategy)` – adiciona uma instância de estratégia à lista de sincronização. Retorna `true` quando registrado com sucesso.
- `UnregisterLinkedStrategy(Strategy strategy)` – remove uma estratégia da lista.
- `ChangeSyncSecurity(Security security)` – alterna para a instância de segurança fornecida e a propaga para todas as estratégias vinculadas.
- `ChangeSyncSecurity(string securityId)` – resolve o identificador por meio do `SecurityProvider` atual e chama `ChangeSyncSecurity(Security)`.
- `ResetToInitialSecurity()` – restaura o símbolo capturado na inicialização.
- `SyncSymbols()` – força uma ressincronização manual sem alterar o identificador armazenado.

## Fluxo de trabalho de uso
1. Instancie `SymbolSyncStrategy` e defina o `Security` primário ou atribua `SyncSecurityId` antes de iniciar a estratégia.
2. Chame `RegisterLinkedStrategy` para cada estratégia filha que deve espelhar o símbolo ativo (por exemplo, diferentes períodos de tempo ou painéis).
3. Sempre que o símbolo principal mudar, chame `ChangeSyncSecurity(Security)` ou `ChangeSyncSecurity(string)`.
4. Opcionalmente, chame `SyncSymbols()` para forçar a propagação se um componente externo modificou uma estratégia vinculada.

## Diferenças em comparação com a versão MQL
- Funciona com StockSharp `Strategy` instâncias em vez de MetaTrader janelas de gráfico.
- Usa a abstração `SecurityProvider` para resolver identificadores.
- Adiciona registro defensivo e um limite configurável para estratégias sincronizadas.
- Oferece métodos explícitos de redefinição e sincronização manual para cenários de automação avançados.

## Notas
- A estratégia não emite ordens de mercado; funciona como um auxiliar de infraestrutura.
- Todos os comentários do código são mantidos em inglês para atender aos requisitos do projeto.
