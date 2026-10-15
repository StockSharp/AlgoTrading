# Estratégia de Força de Divisas CCFp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia porta o clássico consultor especialista CCFp do MetaTrader para a API de alto nível do StockSharp. Ela calcula uma pontuação de força relativa para as oito principais divisas (USD, EUR, GBP, CHF, JPY, AUD, CAD, NZD) usando razões entre médias móviles simples rápidas e lentas nos sete pares principais baseados em USD (EURUSD, GBPUSD, AUDUSD, NZDUSD, USDCAD, USDCHF, USDJPY). Quando a diferença entre duas forças de divisas ultrapassa um limiar configurável, a estratégia abre posições de mercado que expressam a divisa mais forte contra a mais fraca.

A implementação segue a arquitetura de alto nível recomendada: cada instrumento tem sua própria assinatura de candles, os indicadores são vinculados via `Bind`, e o gerenciamento de ordens usa `RegisterOrder` com ordens de mercado. Os comentários nas ordens executadas reutilizam o formato `(TOPDOWN)` original para manter o mesmo estilo de contabilidade que a versão MQL.

## Instrumentos necessários
Anexar os seguintes valores aos parâmetros da estratégia:

- `EURUSD`
- `GBPUSD`
- `AUDUSD`
- `NZDUSD`
- `USDCAD`
- `USDCHF`
- `USDJPY`

Todos os sete pares devem compartilhar o mesmo período definido através do parâmetro `Candle Type`.

## Parâmetros
| Parâmetro | Descrição |
| --- | --- |
| `Fast MA` | Período de média móvel rápida usado no cálculo de força. |
| `Slow MA` | Período de média móvel lenta usado no cálculo de força. |
| `Strength Step` | Diferença mínima entre duas divisas que deve ser excedida para acionar um novo sinal. |
| `Close Opposite` | Se habilitado, a estratégia fecha posições opostas antes de enviar uma nova ordem. |
| `Candle Type` | Série de candles processada pelos indicadores. |
| `Volume` base | Tomado da propriedade padrão `Strategy.Volume` e usado para cada ordem de mercado enviada. |

## Lógica de trading
1. Cada um dos sete pares principais USD é assinado com seu próprio par de médias móviles simples (rápida e lenta).
2. Cada vez que um candle concluído chega, a estratégia converte a razão das médias lenta e rápida nos mesmos valores de força sintéticos produzidos pelo indicador CCFp original.
3. Depois de todos os sete pares serem atualizados, as oito pontuações de força de divisas são recalculadas.
4. Quando a diferença entre uma divisa "top" e uma "down" cruza para cima o nível `Strength Step`, enquanto a divisa top está subindo e a divisa down está caindo, uma oportunidade é detectada.
5. A estratégia abre ordens de mercado que expressam exposição comprada à divisa forte e exposição vendida à divisa fraca:
   - Se USD é a divisa forte, apenas uma ordem é colocada no par contraparte (por exemplo, short `EURUSD`).
   - Se USD é a divisa fraca, a estratégia compra o par onde a divisa forte é a base (por exemplo, long `EURUSD`).
   - Quando ambas as divisas são não-USD, a estratégia envia duas ordens: long a divisa top contra USD e short a divisa down contra USD.
6. Se `Close Opposite` está habilitado e uma posição oposta ainda está aberta em um par alvo, a estratégia envia primeiro uma ordem de mercado de fechamento antes de entrar em um novo trade.

## Gestão de risco
- A estratégia não anexa ordens explícitas de stop-loss ou take-profit; o controle de risco é tratado pelo flag `Close Opposite` junto com ferramentas de gerenciamento de portfólio manual.
- O tamanho de entrada é controlado pela propriedade `Volume`. Configurá-la de acordo com o tamanho da conta e a exposição desejada por segmento.

## Diferenças em relação à implementação MQL original
- O cálculo de força de divisas usa indicadores `SimpleMovingAverage` do StockSharp em um único período. O empilhamento de coeficientes de múltiplos períodos do indicador MQL pode ser emulado ajustando os períodos `Fast MA` e `Slow MA`.
- Os stops protetores não são trailing automaticamente; em vez disso, a estratégia se concentra em reproduzir a lógica de entrada/saída e deixa o controle avançado de risco para a camada de portfólio do StockSharp.
- O roteamento de ordens usa o auxiliar de alto nível `RegisterOrder` e as referências de segurança do StockSharp em vez de objetos de trade do MetaTrader.
