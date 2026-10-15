# Estratégia MACD Simple Reshetov
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia reproduz o comportamento do consultor especialista "MACDSimple" de Yury Reshetov do MetaTrader dentro do framework StockSharp. Trabalha com um único instrumento e avalia sinais MACD clássicos que são modificados por dois parâmetros de offset. O algoritmo processa apenas velas completas, garantindo que todas as decisões de trading sejam tomadas sobre dados confirmados e evitando o ruído intrabarra.

## Indicadores e cálculos
- **MACD (Moving Average Convergence Divergence)** – a linha MACD e a linha de sinal são calculadas com períodos personalizados:
  - Período da EMA rápida = `SignalPeriod + DF`
  - Período da EMA lenta = `SignalPeriod + DS + DF`
  - Período da linha de sinal = `SignalPeriod`
Os offsets `DF` e `DS` seguem as entradas originais do especialista e permitem ao trader esticar ou comprimir os componentes MACD mantendo sua relação intacta.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | --------- | ------ |
| `Volume` | Tamanho de ordem usado para cada entrada de mercado. | 2 |
| `DF` | Offset adicionado ao comprimento da EMA rápida do MACD. Deve ser zero ou positivo. | 1 |
| `DS` | Offset adicional aplicado ao comprimento da EMA lenta do MACD. Deve ser zero ou positivo. | 2 |
| `SignalPeriod` | Período base do qual os comprimentos de EMA rápida e lenta são derivados. | 10 |
| `CandleType` | Período das velas usadas para análise e trading. | Período de 30 minutos |

## Lógica de trading
### Gerenciamento de posição
1. Em cada vela concluída, a estratégia atualiza o indicador MACD e ignora a barra se o indicador ainda não estiver totalmente formado.
2. Se uma posição **comprada** estiver aberta e a linha MACD cair abaixo de zero, a estratégia fecha toda a posição comprada ao preço de mercado.
3. Se uma posição **vendida** estiver aberta e a linha MACD subir acima de zero, a estratégia fecha toda a posição vendida ao preço de mercado.
4. Após fechar uma posição em uma determinada barra, o algoritmo para de processar essa barra, refletindo o comportamento do consultor especialista original.

### Regras de entrada
1. O algoritmo verifica que tanto a linha MACD quanto a linha de sinal compartilham o mesmo sinal (ambas positivas ou ambas negativas). Sinais mistos não produzem operações.
2. Quando ambas as linhas são **positivas**, uma posição comprada é aberta se a linha MACD estiver acima da linha de sinal.
3. Quando ambas as linhas são **negativas**, uma posição vendida é aberta se a linha MACD estiver abaixo da linha de sinal.
4. As ordens de mercado têm o tamanho configurado com o parâmetro `Volume`. Apenas uma posição pode existir por vez.

### Regras de saída
- As saídas são impulsionadas exclusivamente pela linha MACD cruzando o nível zero contra a posição aberta, conforme descrito na seção de gerenciamento de posição. Nenhuma saída parcial, stop loss ou take profit são implementados por padrão.

## Notas adicionais
- A estratégia opera apenas quando `IsFormedAndOnlineAndAllowTrading()` é satisfeito, garantindo que dados ao vivo estejam disponíveis e o trading esteja habilitado antes de entrar em novas posições.
- Não há gerenciamento de risco automático embutido. Os usuários podem adicionar proteções personalizadas como `StartProtection()` ou combinar a estratégia com controles de risco em nível de portfólio, se desejado.
- Como os parâmetros do MACD são derivados de um único período base mais offsets, ajustar `SignalPeriod`, `DF` ou `DS` afeta todos os componentes simultaneamente, preservando o espaçamento relativo pretendido pelo consultor especialista original.

## Detalhes de implementação
- A ligação de indicadores usa a API `SubscribeCandles().Bind()` de alto nível do StockSharp, mantendo a implementação concisa e orientada a eventos.
- A conversão segue o conjunto de regras descrito em `AGENTS.md`: tabulações são usadas para indentação, valores de indicadores são consumidos diretamente do callback de ligação, e funções de trading `BuyMarket`/`SellMarket` gerenciam entradas e saídas.
- A estrutura da estratégia está pronta para extensão (por exemplo, adicionando filtros ou lógica de risco) enquanto permanece fiel à lógica do especialista MetaTrader original.
