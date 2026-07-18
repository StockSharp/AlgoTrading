# Estratégia de Fractals em Preços de Fechamento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port para StockSharp do consultor especialista de MetaTrader 5 **"Fractals at Close prices"** de Vladimir Karputov. Ela analisa cinco preços de fechamento consecutivos para detectar fractais no estilo de Bill Williams construídos estritamente sobre fechamentos em vez de máximos ou mínimos. Os dois fractais de alta e de baixa mais recentes são comparados para determinar a tendência ativa. Quando o último fractal de alta aparece acima do anterior, a estratégia abre uma posição comprada. Quando o último fractal de baixa se forma abaixo do anterior, abre uma posição vendida. Posições opostas são sempre fechadas antes de entrar em uma nova negociação, portanto a estratégia permanece em no máximo uma direção por vez.

As negociações são permitidas apenas entre a hora de início e a hora de término configuráveis. Se a hora atual cair fora dessa janela, todas as posições abertas são fechadas imediatamente, replicando o comportamento do EA original. O filtro de tempo suporta janelas intradiárias (início < fim), sessões noturnas que cruzam a meia-noite (início > fim) e trading durante todo o dia (início == fim).

## Lógica do indicador
* Cada vela finalizada é adicionada a uma fila deslizante de cinco elementos de preços de fechamento.
* Uma vez disponíveis cinco valores, o fechamento intermediário (duas velas atrás) é avaliado:
  * Um fractal de alta é registrado se o fechamento intermediário for estritamente maior que os dois fechamentos mais antigos e maior ou igual aos dois fechamentos mais novos.
  * Um fractal de baixa é registrado se o fechamento intermediário for estritamente menor que os dois fechamentos mais antigos e menor ou igual aos dois fechamentos mais novos.
* Os fractais de alta e de baixa mais recentes e anteriores são armazenados para comparação posterior.
* Uma tendência de alta é detectada quando o último fractal de alta é mais alto que o anterior. Uma tendência de baixa é detectada quando o último fractal de baixa é mais baixo que o anterior.

## Regras de trading
1. **Entradas compradas**
   * Fechar qualquer posição vendida ativa a mercado.
   * Se não houver posição comprada aberta, comprar `OrderVolume` a mercado no fechamento que confirmou a sequência de fractal de alta.
2. **Entradas vendidas**
   * Fechar qualquer posição comprada ativa a mercado.
   * Se não houver posição vendida aberta, vender `OrderVolume` a mercado quando uma sequência de fractal de baixa for confirmada.
3. **Controle de sessão**
   * Antes de aplicar sinais, a estratégia verifica se `candle.OpenTime.Hour` está dentro da janela de trading. Caso contrário, `CloseAllPositions` é chamado e a barra é ignorada.

## Gestão de risco
* As distâncias de stop-loss e take-profit são expressas em pips. A implementação reproduz a abordagem MT5: o ponto do símbolo é multiplicado por dez quando o instrumento tem 3 ou 5 decimais. O valor pip resultante é então multiplicado pelas distâncias configuradas.
* Ao entrar em uma posição, os níveis iniciais de stop-loss e take-profit são armazenados internamente. Como o StockSharp não gerencia automaticamente ordens protetoras no estilo MT5, a estratégia monitora velas finalizadas e sai a mercado quando seu intervalo de preços toca o nível armazenado.
* Os trailing stops seguem as regras originais do EA. Um novo stop é calculado como `close ± TrailingStop` assim que o lucro superar `TrailingStop + TrailingStep`. O trailing stop só avança se o movimento do stop anterior for de pelo menos `TrailingStep`.
* Quando o horário de trading termina, todas as posições são fechadas independentemente do status do trailing. Isso replica o EA chamando `CloseAllPositions` fora da sessão permitida.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `OrderVolume` | Volume usado para cada ordem a mercado. | `0.1` |
| `StartHour` | Hora (0-23) em que o trading se torna ativo. Se igual a `EndHour`, a estratégia opera o dia todo. | `10` |
| `EndHour` | Hora (0-23) em que o trading para de aceitar novos sinais. | `22` |
| `StopLossPips` | Distância do stop-loss expressa em pips. `0` desativa o stop. | `30` |
| `TakeProfitPips` | Distância do take-profit expressa em pips. `0` desativa o take. | `50` |
| `TrailingStopPips` | Distância base do trailing stop em pips. `0` desativa o trailing. | `15` |
| `TrailingStepPips` | Lucro adicional (em pips) necessário antes que o trailing stop avance. | `5` |
| `CandleType` | Tipo de dados de velas assinado pela estratégia. O padrão é velas de período de 1 hora. | `1 hour TimeFrame` |

## Notas de implementação
* A estratégia usa `SubscribeCandles` com a API de alto nível e não registra indicadores manualmente, seguindo as diretrizes do projeto.
* Saídas protetoras (stop, take-profit, trailing stop) são executadas enviando ordens a mercado após uma vela terminar, pois o StockSharp não gerencia automaticamente as ordens protetoras do MT5.
* Filtragem de sessão, detecção de fractais e lógica de trailing seguem estritamente a estrutura do EA, incluindo o fechamento de todas as posições quando o filtro de hora não é satisfeito.
* A lógica de escala de pips espelha a implementação MT5 multiplicando o ponto do símbolo por dez em instrumentos de 3 ou 5 decimais, garantindo distâncias de preço equivalentes.

## Dicas de uso
1. Anexar a estratégia a um símbolo e definir `OrderVolume` para o tamanho de lote preferido.
2. Escolher um tipo de vela que corresponda ao período usado no MetaTrader 5 (o EA original funciona em qualquer período).
3. Ajustar a janela de trading para a sessão do corretor ou horas desejadas.
4. Ajustar as distâncias baseadas em pips para refletir a volatilidade do instrumento. `TrailingStepPips` maior reduz a frequência do trailing, enquanto valores menores fazem o stop seguir o preço mais de perto.
5. Monitorar registros para entradas e saídas; a estratégia desenha negociações na área de gráfico opcional para validação visual rápida.
