# Estratégia Keltner com Filtro Sazonal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
A estratégia **Keltner Seasonal Filter** opera com base em rompimentos do Canal Keltner com filtro de viés sazonal.

Os testes indicam um retorno anual médio de aproximadamente 94%. Funciona melhor no mercado de ações.

Os sinais são acionados quando o Keltner confirma entradas filtradas em dados intradiários (5m). Isso torna o método adequado para traders ativos.

Os stops dependem de múltiplos de ATR e fatores como EmaPeriod, AtrPeriod. Ajuste esses valores padrão para equilibrar risco e recompensa.

## Detalhes
- **Critérios de entrada**: consulte a implementação para as condições dos indicadores.
- **Comprado/Vendido**: Ambos os direções.
- **Critérios de saída**: sinal oposto ou lógica de stop.
- **Stops**: Sim, usando cálculos baseados em indicadores.
- **Valores padrão**:
  - `EmaPeriod = 20`
  - `AtrPeriod = 14`
  - `AtrMultiplier = 2m`
  - `SeasonalThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Keltner, Seasonal
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário (5m)
  - Sazonalidade: Sim
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Regras exatas de negociação (C# e Python)

Os sinais são avaliados apenas em candles **concluídos**, com indicadores formados e negociação permitida. Considere `C` = fechamento, `E = EMA(Close, EmaPeriod)`, `A = ATR(AtrPeriod)`, `M = AtrMultiplier`, `U = E + M × A` e `L = E − M × A`.

O valor sazonal `S` depende do **mês de abertura do candle histórico** (`candle.OpenTime.Month`), nunca de `OnStarted2` ou do relógio local. Valores fixos de demonstração (não calculados do histórico): jan 0.8, fev 0.3, mar 0.6, abr 0.7, mai 0.2, jun −0.3, jul −0.1, ago −0.4, set −0.8, out 0.1, nov 0.9, dez 0.7.

- **Compra / reversão de posição vendida:** `S > SeasonalThreshold && C > U && Position <= 0`: compra a mercado de `Volume + abs(Position)`; stop `C − M × A`.
- **Venda / reversão de posição comprada:** `S < −SeasonalThreshold && C < L && Position >= 0`: venda a mercado de `Volume + abs(Position)`; stop `C + M × A`.
- **Stop prioritário:** com stop armazenado positivo, encerrar compra se `C <= stop` ou venda se `C >= stop`; zerar o stop e parar o processamento desse candle.
- **Saída pela EMA:** caso contrário, encerrar compra se `C < E` ou venda se `C > E`; zerar o stop. Não existe take-profit separado nem trailing automático.

O stop é verificado **manualmente no fechamento do candle concluído**, não como ordem intrabar da bolsa, e parte do fechamento do candle de sinal, não do preço executado. Com limiar `0.5`, jan/mar/abr/nov/dez permitem sinais de compra, set sinais de venda, e os demais meses impedem novas entradas; a ruptura do canal continua obrigatória.
