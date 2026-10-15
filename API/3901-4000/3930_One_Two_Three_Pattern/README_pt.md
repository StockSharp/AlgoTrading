# Estratégia de padrão um-dois-três
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia reproduz o consultor especialista MetaTrader 4 “1-2-3_forCodeBase_v01.mq4” de Martes. Ele verifica as velas finalizadas em busca do padrão clássico de reversão 1-2-3: duas pernas de tendência consecutivas completadas por uma terceira perna de retração. A porta mantém todas as regras do sistema original, incluindo os indicadores personalizados de comprimento de tendência (`RelDownTrLen_forCodeBase_v01` e `RelUpTrLen_forCodeBase_v01`) e a lógica de confirmação MACD.

Uma configuração longa requer um novo vale (ponto 3) próximo ao preço atual, um pico anterior (ponto 2) e um vale mais antigo (ponto 1). A tendência de baixa anterior deve ser pelo menos `TrendRatio` vezes mais longa que a retração de alta atual e MACD deve cruzar acima da linha de sinal (ou zero) enquanto permanece positiva no ponto 3. O lado curto reflete essas verificações com picos e vales invertidos. Os stops são colocados um ponto além do ponto 3, o take-profit é igual à altura da oscilação anterior e um trailing stop opcional baseado em pip aperta a saída quando a negociação passa para o lucro.

## Regras de negociação

1. Assine a série de velas configurada (`CandleType`) e calcule MACD (períodos rápido/lento/sinal) nos preços de fechamento.
2. Mantenha um histórico contínuo dos corpos das velas para detectar a estrutura 1-2-3. Os vales são os mínimos locais dos corpos das velas, os picos são os máximos locais.
3. Avalie as métricas de comprimento de tendência personalizadas usando o método de casco convexo dos indicadores MQL. O último comprimento da tendência de baixa (escalado para `[0,1]`) deve dominar a tendência de alta anterior (e vice-versa para posições vendidas) de acordo com `TrendRatio`.
4. Confirme a configuração com MACD:
   - Longo: `MACD` cruza acima do sinal (ou acima de zero) e o valor MACD no ponto 3 é positivo.
   - Curto: `MACD` cruza abaixo do sinal (ou abaixo de zero) e o valor MACD no ponto 3 é negativo.
5. Filtros de entrada adicionais:
   - A distância do preço atual ao ponto 2 deve estar dentro de cinco pontos.
   - A distância de parada projetada (`|point2 - point3|`) deve ser de pelo menos 13 pontos.
   - `TakeProfitPips` deve permanecer ≥ 10; caso contrário, a negociação será desativada (espelha a verificação de segurança original).
6. Tratamento de pedidos:
   - Insira usando `BuyMarket`/`SellMarket` com `TradeVolume` lotes (agregados com o volume da posição atual para reversões).
   - Stop loss inicial = ponto 3 ± uma etapa de preço.
   - Obter lucro = entrada ± `|point2 - point3|`.
   - Se `TrailingStopPips` > 0, siga o stop por esse número de pontos quando o lucro não realizado exceder a distância final.
7. Saia em stop, takeprofit ou trailing stop. Apenas uma posição pode ser aberta por vez.

## Parâmetros

| Parâmetro | Tipo | Padrão | Descrição |
|-----------|------|---------|-------------|
| `TakeProfitPips` | `decimal` | `60` | Sinalizador de compatibilidade do EA. A negociação é interrompida se o valor for definido abaixo de 10. |
| `TradeVolume` | `decimal` | `0.5` | Volume em MetaTrader lotes utilizados para cada ordem de mercado. |
| `TrailingStopPips` | `decimal` | `30` | Distância da parada final em MetaTrader pontos. Defina como `0` para desativar o rastreamento. |
| `TrendRatio` | `decimal` | `4` | Relação mínima entre o comprimento da tendência principal anterior e a retração recente. |
| `CandleType` | `DataType` | `H1` | Série de velas usada para cálculos de padrão e MACD. |
| `MacdFast` | `int` | `12` | Período EMA rápido do oscilador MACD. |
| `MacdSlow` | `int` | `26` | Período EMA lento do oscilador MACD. |
| `MacdSignal` | `int` | `9` | Período da linha de sinal EMA. |
| `PatternLookback` | `int` | `100` | Número máximo de velas históricas digitalizadas ao localizar os pontos 1-2-3. |

## Notas de implementação

- Os indicadores personalizados originais são portados literalmente: pesquisas de casco convexo calculam os segmentos monotônicos mais longos dos corpos das velas e retornam seus comprimentos relativos em `[0,1]`. Esses valores orientam o filtro de taxa de tendência.
- Velas históricas e valores MACD são armazenados em buffers limitados (600 elementos) para evitar o uso excessivo de memória, mantendo profundidade suficiente para o lookback.
- Stops e metas são gerenciados manualmente para corresponder ao comportamento MetaTrader: os preços são comparados com os máximos/mínimos das velas, e o trailing stop só se estreita quando o preço avança pelo menos na distância configurada.
- `Volume` é sincronizado com `TradeVolume` na reinicialização e na inicialização, portanto, a otimização pode contar com a propriedade de estratégia padrão.

## Referências

- Consultor especialista MQL4 original: `MQL/8131/1-2-3_forCodeBase_v01.mq4`.
- Indicadores personalizados: `RelDownTrLen_forCodeBase_v01.mq4`, `RelUpTrLen_forCodeBase_v01.mq4`.
