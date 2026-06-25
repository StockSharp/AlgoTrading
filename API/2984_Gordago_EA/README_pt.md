# Estratégia Gordago EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Um port do histórico consultor especialista "Gordago EA" do MetaTrader 5. A estratégia opera no período base (padrão M3) enquanto lê sinais MACD de um gráfico intradiário superior e um filtro estocástico de um gráfico horário. Preserva os parâmetros originais de stop/take e a lógica de trailing, mas usa a API de alto nível do StockSharp para assinaturas de dados e gerenciamento de ordens.

## Lógica da estratégia

- **Dados de mercado**
  - Candles de execução principal: configuráveis, padrão candles de três minutos.
  - Candles MACD: configuráveis, padrão candles de doze minutos.
  - Candles estocásticos: configuráveis, padrão candles de uma hora.
- **Indicadores**
  - MACD (rápido 12, lento 26, sinal 9) calculado no período MACD.
  - Oscilador estocástico (comprimento 5, suavização %K 3, %D 3) calculado no período estocástico.
- **Condições de entrada**
  - **Comprar**: valor MACD atual acima do anterior, MACD anterior abaixo de zero, %K estocástico abaixo do limiar de compra (padrão 37) e subindo em relação ao valor anterior.
  - **Vender**: valor MACD atual abaixo do anterior, MACD anterior acima de zero, %K estocástico acima do limiar de venda (padrão 96) e caindo em relação ao valor anterior.
- **Colocação de ordens**
  - O volume da ordem é fixo; mudar de direção compensa automaticamente qualquer posição oposta antes de abrir uma nova.
  - Existem distâncias separadas de stop-loss/take-profit para operações compradas e vendidas (padrões: 40/70 pips para comprado, 10/40 pips para vendido).
- **Saídas**
  - Níveis protetores de stop-loss e take-profit são verificados em cada candle base finalizado.
  - Um trailing stop se ativa quando o preço avança além da distância de trailing configurada mais o passo de trailing; uma vez acionado, ele continua avançando em direção ao mercado pela distância de trailing.
  - O trailing pode introduzir um stop de proteção mesmo quando o stop original estava desabilitado, espelhando o EA fonte.

## Parâmetros

- `OrderVolume` – volume de negociação em lotes.
- `StopLossBuyPips` / `TakeProfitBuyPips` – distâncias de stop-loss e take-profit para o lado comprado (em pips).
- `StopLossSellPips` / `TakeProfitSellPips` – distâncias de stop-loss e take-profit para o lado vendido (em pips).
- `TrailingStopPips` – distância de trailing em pips; definir como zero para desabilitar o trailing.
- `TrailingStepPips` – lucro adicional mínimo (em pips) antes que o trailing stop possa avançar.
- `StochasticBuyLevel` / `StochasticSellLevel` – limiares do oscilador para entradas compradas e vendidas.
- `CandleType` – período de trabalho para a lógica de execução.
- `MacdCandleType` – período usado para alimentar o indicador MACD.
- `StochasticCandleType` – período usado para alimentar o oscilador estocástico.
- `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` – períodos MACD.
- `StochasticLength`, `StochasticSignalPeriod`, `StochasticSmoothing` – períodos do oscilador estocástico.

## Notas de implementação

- Distâncias em pips são convertidas para preços usando o `PriceStep` do instrumento. Se o passo tiver três ou cinco dígitos fracionários, a estratégia o multiplica por dez, reproduzindo o ajuste de pip usado na implementação MQL original para cotações forex de 3/5 dígitos.
- O trailing stop é ignorado quando `TrailingStopPips` é positivo mas `TrailingStepPips` não é; nesse caso um aviso é registrado.
- Como a versão StockSharp trabalha com eventos de fechamento de candle, a lógica de proteção é executada uma vez por candle finalizado em vez de a cada tick como na versão MT5. O comportamento de gerenciamento de negociação segue as regras originais.
- Apenas a implementação em C# é fornecida; nenhuma tradução ou pasta Python está incluída por solicitação.
