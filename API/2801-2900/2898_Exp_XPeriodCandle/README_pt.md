# Estratégia Exp XPeriodCandle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port StockSharp do consultor especialista MQL5 `Exp_XPeriodCandle`. Reconstrói o indicador personalizado XPeriodCandle com componentes de API de alto nível e usa transições de cor de candle para abrir e fechar posições.

## Conceito

* Suavizar a abertura, máxima, mínima e fechamento de cada candle concluído usando uma aproximação de média móvel configurável.
* Rastrear a "cor do candle" resultante (altista se o fechamento suavizado estiver acima da abertura suavizada, baixista caso contrário).
* Usar a cor dos últimos dois candles concluídos (deslocamento configurável) para detectar reversões e emitir sinais de trading.
* Opcionalmente fechar posições opostas quando um novo sinal aparece e aplicar níveis de stop-loss/take-profit protetores expressos em pontos de preço.

## Detalhes de implementação

* Tipos de suavização diretamente suportados: Simple, Exponential, Smoothed (RMA) e Linear Weighted. Todas as outras opções são aproximadas com um suavizador exponencial porque StockSharp não inclui equivalentes diretos de JJMA/JurX/Parabolic/T3/VIDYA/AMA. Documentado em comentários de código para manter o comportamento transparente.
* Filas deslizantes armazenam as últimas `Period` máximas e mínimas suavizadas para manter o intervalo de preço consistente com o indicador original.
* A estratégia aguarda até que haja histórico suficiente disponível antes de chamar `BuyMarket`/`SellMarket` e se marca como formada para trabalhar com os filtros de backtesting do StockSharp.
* Conversões opcionais de slippage, stop-loss e take-profit dependem do passo de preço do instrumento. Quando o passo é desconhecido, os valores de pontos brutos são usados.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `CandleType` | Período dos candles processados. |
| `Period` | Profundidade da janela de suavização (igual ao período do indicador). |
| `SmoothingMethods` | Aproximação de média móvel usada para todas as séries OHLC. Métodos não suportados voltam para EMA. |
| `SmoothingLength` | Parâmetro de comprimento para o suavizador. |
| `SmoothingPhase` | Entrada de fase adicional (mantida para completude; apenas ativa na família JJMA original do MQL). |
| `SignalBar` | Qual candle concluído avaliar (1 = candle anterior, replicando o padrão do especialista MQL). |
| `EnableLongEntry` / `EnableShortEntry` | Permitir abertura de posições na direção correspondente. |
| `EnableLongExit` / `EnableShortExit` | Fechar posições existentes quando um sinal oposto é detectado. |
| `StopLossPoints` / `TakeProfitPoints` | Saídas protetoras expressas em pontos de preço. Definir como zero para desabilitar. |
| `SlippagePoints` | Slippage permitido em pontos de preço aplicado a ordens de mercado. |

## Regras de trading

1. Suavizar o último candle concluído e adicionar sua cor ao histórico rotativo.
2. Quando as cores de `SignalBar` e mais antigas existem:
   * Se o candle mais antigo era altista (cor < 1) e o candle mais novo não é altista (cor > 0), abrir posição comprada (se permitido) e opcionalmente fechar vendidos.
   * Se o candle mais antigo era baixista (cor > 1) e o candle mais novo não é baixista (cor < 2), abrir posição vendida (se permitido) e opcionalmente fechar comprados.
3. O tamanho da posição segue a configuração `Volume` da estratégia; a exposição oposta é zerada antes de reverter.
4. A gestão de risco é tratada por `StartProtection` usando as distâncias de pontos fornecidas.

## Notas

* O especialista original usa o `SmoothAlgorithms.mqh` proprietário. Como StockSharp não possui implementações diretas de JJMA/JurX/T3, a conversão em C# aproxima esses modos com suavização exponencial. Este comportamento é documentado em comentários de código e no README para que os otimizadores possam ajustar os parâmetros se necessário.
* As entradas e valores padrão espelham a versão MQL, permitindo intervalos de otimização similares.
