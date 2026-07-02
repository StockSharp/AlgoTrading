# Estratégia de média móvel ajustável
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia recria o consultor especialista MetaTrader "Média Móvel Ajustável" usando o API de alto nível de StockSharp. Duas médias móveis do mesmo tipo, mas com comprimentos diferentes, monitoram sua distância. Quando a curva mais rápida cruza a mais lenta por pelo menos um intervalo configurável, a estratégia fecha qualquer posição oposta e, opcionalmente, abre uma negociação na nova direção. Filtros de sessão adicionais, saídas de proteção e um trailing stop opcional proporcionam a mesma flexibilidade operacional do robô original.

## Lógica de negociação

- Duas médias móveis (rápida e lenta) compartilham o mesmo método de cálculo. O período mais rápido é definido automaticamente para a entrada menor, o período mais lento para a entrada maior.
- Um sinal é produzido somente depois que ambas as médias móveis estiverem totalmente formadas e sua distância absoluta exceder o limite `MinGapPoints` convertido em unidades de preço.
- Quando a MM rápida está acima da MM lenta no intervalo necessário, o estado do sinal interno torna-se altista. Um estado de baixa é registrado quando a MM lenta está acima da MM rápida.
- Uma mudança de estado fecha qualquer posição existente se `CloseOutsideSession` estiver ativado ou o horário atual estiver dentro da janela da sessão. Novos pedidos seguem o `Mode` selecionado (somente compra, somente venda ou ambos) e usam um lote fixo ou a regra de dimensionamento de lote automático.
- A lógica de proteção é verificada em cada vela acabada:
  - As distâncias de stop loss e takeprofit são medidas em pontos do instrumento e avaliadas em relação ao intervalo de velas.
  - O trailing stop é ativado quando o preço se move a favor da posição em pelo menos `TrailStopPoints` pontos. A parada é reduzida somente quando o filtro de sessão permite rastreamento ou `TrailOutsideSession` está ativado. Uma vez estabelecida a parada, ela permanece ativa mesmo fora do horário de negociação.

## Dimensionamento de posição

- Com `EnableAutoLot = false` a estratégia envia o volume `FixedLot` (após aplicar a etapa do instrumento, limites mínimo e máximo).
- Com `EnableAutoLot = true` o volume é aproximado do valor do portfólio disponível: `(PortfolioValue / 10,000) * LotPer10kFreeMargin`, arredondado para um lote decimal. O volume computado também está alinhado às restrições cambiais.

## Parâmetros

| Nome | Tipo/Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | `TimeFrame` = velas de 5 minutos | Período usado para cálculos de média móvel. |
| `FastPeriod` | `int` = 3 | Comprimento médio móvel curto. Deve ser diferente de `SlowPeriod`. |
| `SlowPeriod` | `int` = 9 | Comprimento médio móvel longo. Deve ser diferente de `FastPeriod`. |
| `MaMethod` | `MovingAverageMethod` = Exponencial | Algoritmo de média móvel (simples, exponencial, suavizado, ponderado). |
| `MinGapPoints` | `decimal` = 3 | Distância mínima entre as médias rápida e lenta nos pontos do instrumento. Convertido usando a etapa de preço do instrumento. |
| `StopLossPoints` | `decimal` = 0 | Distância de parada protetora nos pontos do instrumento. Defina como zero para desativar. |
| `TakeProfitPoints` | `decimal` = 0 | Distância alvo de lucro em pontos de instrumento. Defina como zero para desativar. |
| `TrailStopPoints` | `decimal` = 0 | Distância de parada final em pontos do instrumento. Defina como zero para desativar. |
| `Mode` | `EntryMode` = Ambos | Direção permitida para novas negociações (Ambos, BuyOnly, SellOnly). |
| `SessionStart` | `TimeSpan` = 00:00 | Hora de início da sessão (relógio da plataforma). |
| `SessionEnd` | `TimeSpan` = 23:59 | Hora de término da sessão (relógio da plataforma). Suporta sessões noturnas quando `SessionEnd < SessionStart`. |
| `CloseOutsideSession` | `bool` = verdadeiro | Se for verdade, as posições opostas serão fechadas mesmo fora da janela da sessão. |
| `TrailOutsideSession` | `bool` = verdadeiro | Se for verdade, o trailing stop continua sendo atualizado após o fechamento da sessão. |
| `FixedLot` | `decimal` = 0,1 | Volume usado quando o dimensionamento automático está desativado. |
| `EnableAutoLot` | `bool` = falso | Ative a estimativa de volume a partir do valor do portfólio. |
| `LotPer10kFreeMargin` | `decimal` = 1 | Lotes alocados por 10.000 unidades de valor do portfólio no modo lote automático. |
| `MaxSlippage` | `int` = 3 | Retido para integridade; StockSharp ordens de mercado não expõem um parâmetro de derrapagem direto. |
| `TradeComment` | `string` = "AdjustableMovingAverageEA" | Texto incluído nas mensagens de log quando as negociações são executadas. |

## Notas

- A versão original do MetaTrader aplicava stop loss, takeprofit e trailing stops por meio de modificações de pedidos. A porta StockSharp emula o comportamento avaliando intervalos de velas e enviando ordens de mercado opostas.
- O valor do portfólio é usado como uma aproximação da margem livre porque o `AccountFreeMargin()` de MetaTrader não está disponível em StockSharp.
- Quando o instrumento não possui um `PriceStep` válido, os cálculos baseados em pontos (gap, stops, trailing) permanecem inativos.
