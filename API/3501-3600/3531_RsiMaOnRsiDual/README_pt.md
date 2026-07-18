# RSI MA em RSI Estratégia Dupla
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

O RSI MA em RSI estratégia dupla recria o MetaTrader consultor especialista "RSI_MAonRSI_Dual" dentro de StockSharp. Ele observa dois índices de força relativa com diferentes períodos de lookback e aplica uma média móvel comum no topo de cada fluxo RSI. As decisões de negociação são tomadas quando as linhas RSI suavizadas se cruzam enquanto permanecem no mesmo lado de um nível neutro configurável.

A conversão mantém o comportamento do robô original, incluindo filtragem de tempo e a capacidade de restringir a direção de negociação ou reverter a lógica do sinal.

## Indicadores

- **Rápido RSI** – Índice de Força Relativa com período configurável.
- **Lento RSI** – Índice de Força Relativa com período próprio.
- **Média móvel em RSI** – Média móvel simples calculada sobre cada fluxo de valor RSI. Ambos os RSIs usam o mesmo comprimento de suavização.

Todos os três indicadores compartilham o mesmo preço aplicado (preço de fechamento por padrão). As duas linhas RSI suavizadas são desenhadas em um painel gráfico dedicado para monitoramento.

## Regras de entrada

1. Aguarde até que ambos os valores RSI suavizados se formem na barra atualmente concluída.
2. **Configuração longa**
   - O RSI suavizado rápido cruza **acima** o RSI suavizado lentamente (valor atual acima, valor anterior abaixo).
   - Ambos os RSIs suavizados estão **abaixo** do nível neutro (50 por padrão).
3. **Configuração curta**
   - O RSI suavizado rápido cruza **abaixo** o RSI suavizado lentamente (valor atual abaixo, valor anterior acima).
   - Ambos os RSIs suavizados estão **acima** do nível neutro.
4. Opcionalmente, inverta as direções do sinal usando o parâmetro `ReverseSignals`.
5. Os sinais gerados na mesma barra são ignorados (uma entrada por barra).

## Gestão de posição

- `AllowLong` e `AllowShort` controlam se a estratégia pode abrir posições em cada direção.
- `CloseOpposite` fecha uma posição existente antes de entrar no lado oposto, replicando a lógica original EA.
- `OnlyOnePosition` proíbe a abertura de uma nova posição quando qualquer posição já estiver ativa.
- As ordens de mercado são emitidas com a estratégia `Volume`.

## Filtro de tempo

Ative ou desative o filtro da sessão de negociação com `UseTimeFilter`. Quando ativado, as negociações são permitidas apenas entre `SessionStart` e `SessionEnd`. Sessões que ultrapassam a meia-noite são suportadas. Os carimbos de data/hora são avaliados no fuso horário de troca fornecido pelas mensagens de vela recebidas.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `CandleType` | Série de velas analisada pela estratégia. |
| `FastRsiPeriod` | Período do jejum RSI. |
| `SlowRsiPeriod` | Período da lentidão RSI. |
| `MaPeriod` | Comprimento médio móvel usado para suavizar ambos os fluxos RSI. |
| `AppliedPrice` | Tipo de preço encaminhado para os cálculos de RSI. |
| `NeutralLevel` | RSI limite que separa as zonas de alta e de baixa. |
| `AllowLong` / `AllowShort` | Ative ou desative a direção de negociação. |
| `ReverseSignals` | Troque sinais longos e curtos. |
| `CloseOpposite` | Feche a posição oposta antes de inserir uma nova. |
| `OnlyOnePosition` | Permitir no máximo uma posição aberta. |
| `UseTimeFilter` | Ative o filtro da sessão de negociação. |
| `SessionStart` / `SessionEnd` | Limites da janela de negociação. |

## Diferenças do original EA

- Os blocos de gerenciamento de dinheiro, stop-loss e trailing-stop do código MQL5 original não são reproduzidos. A estratégia StockSharp coloca ordens de mercado usando o `Volume` fixo configurado na estratégia.
- Todos os alertas de registro e diagnóstico foram removidos; O registro StockSharp deve ser usado, se necessário.
- O rastreamento de transações específico da plataforma é substituído por StockSharp eventos de estado de pedido.

Apesar dessas diferenças, a lógica de entrada principal e os filtros direcionais correspondem ao consultor especialista de origem.
