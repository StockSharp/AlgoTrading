# Estratégia RSI RFTL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o **RSI RFTL EA** do MetaTrader 5 para a API de alto nível do StockSharp. Mantém a ideia original de negociar linhas de tendência de swing do RSI, aprimorada com a Recursive Filter Trend Line (RFTL) como filtro direcional. A implementação reproduz a tomada de decisão barra a barra do consultor especialista usando construções idiomáticas do StockSharp como `StrategyParam`, vinculações de indicadores e assinaturas de velas.

## Como Funciona

1. **Detecção de swing do RSI** – os últimos 500 valores de RSI são varridos em busca de máximos e mínimos locais. Os picos devem subir acima de 40 e 60, enquanto os vales devem cair abaixo de 60 e 40, correspondendo à lógica de pontos de inflexão do MQL.
2. **Projeção de linha de tendência** – uma vez encontrados dois máximos ou mínimos válidos, a estratégia projeta a correspondente linha de tendência do RSI para a barra atual e a anterior. Swings intermediários que quebram os limiares 40/60 invalidam a linha, assim como no consultor especialista.
3. **Confirmação RFTL** – o valor anterior da Recursive Filter Trend Line (calculado com a tabela de coeficientes original) deve estar acima do fechamento anterior para shorts ou abaixo para longs. Isso mantém as entradas alinhadas com o filtro RFTL.
4. **Filtragem de entrada** – o RSI também deve permanecer no lado apropriado do neutro: shorts requerem RSI acima de 47/50, enquanto longs requerem RSI abaixo de 55/50.
5. **Camada de risco** – as distâncias de stop de proteção, take-profit e trailing stop são expressas em pips e atualizadas em cada vela concluída, imitando a rotina de modificação de trailing do MQL. Saídas adicionais ocorrem quando RSI supera 70 (fechar longs) ou cai abaixo de 30 (fechar shorts).

## Lógica de Entrada

- **Configuração vendida**
  - Dois mínimos de RSI abaixo de 60/40 definem uma linha de tendência ascendente cuja projeção agora é quebrada para baixo (`RSI[1] < linha`, `RSI[2] > linha(anterior)`).
  - O valor anterior de RFTL está acima do fechamento anterior, confirmando pressão descendente.
  - RSI permanece no lado de alta (`RSI[2] > 50`, `RSI[0] > 47`) e os topos detectados ficam mais atrás na história do que os fundos (`pos₂ > pos₄`), correspondendo à restrição de ordenamento do MQL.
- **Configuração comprada**
  - Dois máximos de RSI acima de 40/60 definem uma linha de tendência descendente cuja projeção agora é quebrada para cima (`RSI[1] > linha`, `RSI[2] < linha(anterior)`).
  - O valor anterior de RFTL está abaixo do fechamento anterior.
  - RSI permanece no lado de baixa (`RSI[2] < 50`, `RSI[0] < 55`) e os fundos recentes são mais recentes do que os topos (`pos₄ > pos₂`).

Os sinais são avaliados apenas após todos os indicadores estarem formados e o histórico necessário ter sido acumulado, evitando negociações prematuras com dados parciais.

## Gerenciamento de Risco

- **Stop Loss / Take Profit** – configuráveis em pips. Se a vela atual negociar além do respectivo nível de preço, a posição é fechada imediatamente e o estado de trailing é reiniciado.
- **Trailing Stop** – opcional. Uma vez que o preço se move por `TrailingStopPips + TrailingStepPips` a favor da negociação, o stop acompanha o fechamento enquanto aplica o mesmo avanço mínimo (`TrailingStepPips`) antes de apertar novamente.
- **Saída de Emergência RSI** – longs fecham quando RSI cruza 70; shorts fecham quando cai abaixo de 30. Isso espelha as saídas hard-coded no EA original.

## Parâmetros

| Parâmetro | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | 1 hora | Período usado para os cálculos de RSI e RFTL. |
| `TradeVolume` | 1 | Volume de ordem enviado em cada entrada. |
| `RsiPeriod` | 30 | Período de lookback do oscilador RSI. |
| `StopLossPips` | 50 | Distância do stop de proteção em pips (0 desativa o stop). |
| `TakeProfitPips` | 50 | Distância do take-profit em pips (0 desativa o alvo). |
| `TrailingStopPips` | 5 | Deslocamento do trailing stop em pips (0 desativa o trailing). |
| `TrailingStepPips` | 5 | Melhoria adicional em pips necessária antes da atualização do trailing. |

Todas as distâncias são multiplicadas pelo `PriceStep` do instrumento, correspondendo ao tratamento de ponto/pip da versão MQL.

## Uso

1. Anexar a estratégia a um ativo e definir `CandleType` para o tamanho de barra usado nos testes do MetaTrader.
2. Ajustar os parâmetros de risco (stop, take, trailing) para as distâncias em pips usadas anteriormente. Definir um parâmetro como `0` desativa essa proteção.
3. Iniciar a estratégia; ela assinará as velas especificadas, calculará RSI e RFTL, e começará a monitorar sinais quando histórico suficiente for coletado.
4. Monitorar os widgets do gráfico – a área de preço exibe velas e a linha RFTL, enquanto o segundo painel mostra o oscilador RSI.

## Notas e Diferenças

- O indicador RFTL está implementado diretamente em C# com a tabela de coeficientes original; nenhum arquivo externo é necessário.
- O gerenciamento de negociações permanece de posição única: a estratégia alterna entre comprado, vendido e sem posição, assim como o EA que rastreava apenas uma posição por símbolo/mágico.
- Como os exits de stop e trailing são gerenciados dentro da estratégia (o StockSharp não executa automaticamente os stops do MT5), as reentradas são puladas na barra onde uma saída de proteção é acionada, o que é uma aproximação conservadora mas segura.
- Os buffers de histórico são limitados a 600 registros para refletir os arrays de 500 elementos usados no código-fonte e evitar crescimento ilimitado de memória.
- Todos os comentários inline foram reescritos em inglês e o código segue as diretrizes de estilo da API de alto nível do StockSharp.
