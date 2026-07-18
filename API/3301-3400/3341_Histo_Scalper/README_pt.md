# Estratégia Histo Scalper
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Histo Scalper Strategy** é uma versão C# do consultor especialista MetaTrader *HistoScalperEA v1.0*. O algoritmo funde oito indicadores de estilo histograma (ADX, ATR, Bollinger Bandas, Bulls/Bears Power, CCI, MACD, RSI e Stochastic) e requer acordo unânime de todos os filtros habilitados antes de abrir uma negociação. Um segundo requisito é que pelo menos um filtro reporte a direção oposta na barra anterior, o que impede a entrada da estratégia durante mercados planos e imita a lógica de confirmação original de “duas barras”.

## Geração de Sinal
1. **ADX filtro** – verifica se +DI é maior que −DI. Opcionalmente, inverta a decisão.
2. **ATR filtro** – compara o ATR atual com uma linha de base SMA e mede o desvio percentual. As negociações longas exigem um desvio positivo acima de `AtrPositiveThreshold`; as negociações curtas exigem um desvio negativo abaixo de `AtrNegativeThreshold`.
3. **Bollinger rompimento** – espera que o preço de fechamento rompa a banda superior/inferior.
4. **Poder Bulls/Bears** – usa Bulls Power para entradas longas e magnitude Bears Power para entradas curtas.
5. **CCI** – é acionado quando o valor CCI ultrapassa os níveis de sobrevenda/sobrecompra configurados.
6. **MACD histograma** – mede a distância entre MACD e sua linha de sinal.
7. **RSI** – usa zonas clássicas de sobrevenda/sobrecompra.
8. **Stochastic** – lê a linha %K e a compara com os limites configurados.

Se algum filtro habilitado produzir um valor neutro, a estratégia aborta o processamento da vela atual. O estado histórico de cada filtro é armazenado para impor a regra da "barra anterior oposta".

## Gestão de risco
* As entradas no mercado usam o parâmetro `TradeVolume`.
* A pirâmide opcional aumenta as posições abertas; caso contrário, a estratégia só muda de direção quando o sinal muda.
* Os níveis de take-profit e stop-loss são expressos em etapas de preço do instrumento e aplicados imediatamente após o envio da ordem via `SetTakeProfit` e `SetStopLoss`.
* Um filtro de sessão (`UseTimeFilter`, `SessionStart`, `SessionEnd`) pode desativar a negociação fora do horário configurado.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Volume base para novas negociações.
| `AllowPyramiding` | Permite empilhar negociações adicionais enquanto já está posicionado.
| `CloseOnOppositeSignal` | Fecha as posições existentes quando o sinal agregado muda.
| `UseTimeFilter`, `SessionStart`, `SessionEnd` | Restringe a negociação a uma janela diária personalizada.
| `UseTakeProfit`, `TakeProfitPoints` | Habilita e configura o lucro em etapas de preço.
| `UseStopLoss`, `StopLossPoints` | Habilita e configura stop loss em etapas de preço.
| `UseIndicator1` … `UseIndicator8` | Habilite filtros individuais.
| `ModeIndicatorX` | Alternar entre lógica direta e invertida para cada filtro.
| Configurações específicas do indicador | Períodos, limites e níveis que replicam as entradas originais do consultor especialista.

## Diferenças do especialista MQL
* O gerenciamento de lucros/perdas da cesta, os alertas sonoros e o gerenciamento de ordens de rede são intencionalmente omitidos.
* A automação de risco (dimensionamento automático de lote, ponto de equilíbrio e lógica de rastreamento) não está incluída; use os parâmetros de risco acima.
* As verificações de spread e as proteções específicas do corretor não são portadas.

## Notas de uso
1. Defina `Security` e `Portfolio` antes de iniciar a estratégia.
2. Ajuste o tipo de vela (`CandleType`) para corresponder ao período desejado.
3. Configure os limites do indicador para se adequarem à volatilidade do instrumento alvo.
4. Ative ou desative filtros individualmente para simplificar a otimização.
5. Use `AllowPyramiding` e `CloseOnOppositeSignal` para controlar a exposição durante mercados rápidos.
