# Estratégia de volatilidade por rompimento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de volatilidade por rompimento procura rajadas curtas de volatilidade intrabar. Ela espera por um candle cujo intervalo se expanda além do candle anterior, mas apenas dentro de uma faixa estreita (dois equivalentes de pip depois da normalização de dígitos). Quando esse candle fecha em alta, a estratégia compra; quando fecha em baixa, ela vende. Stops de proteção, um trailing stop opcional e uma sequência automática de reversão após perda gerenciam o risco e tentam recuperar movimentos adversos.

## Lógica de negociação

1. **Filtro de expansão do intervalo**
   - Calcule o intervalo do candle atual (`High - Low`) e compare-o com o candle anterior.
   - Exija que o intervalo atual seja maior, mas que não exceda o intervalo anterior por mais de dois pips normalizados.
   - Isso cria uma configuração em que a volatilidade está aumentando, mas ainda contida, apontando para um possível rompimento sem ruído excessivo.
2. **Viés direcional**
   - Se o candle fechar acima da abertura, entre comprado.
   - Se o candle fechar abaixo da abertura, entre vendido.
   - A estratégia pode opcionalmente impedir mais de uma entrada por barra para evitar sinais repetidos no mesmo candle.
3. **Gestão da posição**
   - Stop-loss inicial e take-profit são atribuídos em pontos (equivalentes de pip) relativos ao preço de entrada.
   - Um trailing stop opcional aperta o nível de proteção depois que o preço se move uma distância especificada a favor da operação. Um passo de trailing evita ajustes minúsculos.
   - Quando uma posição fecha com perda, a estratégia pode inverter a direção imediatamente. Cada reversão aumenta a distância de take-profit para compensar o risco adicional. Um limite para o número de reversões consecutivas impede comportamento de martingale infinito.

## Parâmetros

| Nome | Descrição | Padrão | Otimizável |
| --- | --- | --- | --- |
| `TradeVolume` | Volume base da ordem para entradas a mercado. | `0.1` | Sim |
| `StopLossPoints` | Distância do stop-loss em pontos. | `20` | Sim |
| `TakeProfitPoints` | Distância do take-profit em pontos. | `10` | Sim |
| `TrailingStopPoints` | Distância do trailing stop em pontos. Defina como `0` para desabilitar. | `25` | Não |
| `TrailingStepPoints` | Passo incremental mínimo ao mover o trailing stop. | `5` | Não |
| `OnlyOnePositionPerBar` | Proíbe múltiplas entradas durante o mesmo candle. | `true` | Não |
| `UseAutoDigits` | Multiplica o tamanho do ponto por 10 para símbolos com 3 ou 5 casas decimais, convertendo para unidades de pip. | `true` | Não |
| `ReverseAfterStop` | Habilita o fluxo de reversão após perda. | `true` | Não |
| `MaxReverseOrders` | Número máximo de operações reversas consecutivas. | `2` | Não |
| `TakeProfitIncrease` | Pontos extras de take-profit adicionados para cada ordem reversa. | `100` | Não |
| `CandleType` | Tipo de candle e período para os cálculos. | `TimeSpan.FromMinutes(1)` | Não |

## Gestão de risco

- Os deslocamentos de stop-loss e take-profit são recalculados usando o passo de preço do instrumento. A detecção automática de dígitos converte cotações de cinco dígitos em distâncias do tamanho de um pip.
- A lógica de trailing só é ativada depois que o mercado avança pela distância de trailing especificada e impõe um passo mínimo antes de modificar o stop.
- A negociação reversa é reiniciada depois de uma saída lucrativa ou depois de atingir o limite configurado de reversões consecutivas.

## Notas práticas

- Funciona melhor em pares de moedas com spreads apertados, onde pequenas mudanças de volatilidade podem indicar rajadas de momentum.
- Considere alinhar o período do candle com a sessão de mercado alvo; o período padrão de 1 minuto captura rompimentos de alta frequência.
- Como as reversões são executadas imediatamente após um fechamento perdedor, garanta que haja margem suficiente para operações consecutivas.
