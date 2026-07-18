# CandleStop Sistema Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento construída em torno do indicador de canal personalizado CandleStop. O sistema calcula continuamente bandas de máxima-máxima e mínima-mínima atrasadas, aguarda um candle completo fechar além dessas bandas e reage na barra seguinte. Opcionalmente impõe um tempo de vida máximo de posição e usa stops de proteção baseados em pontos.

## Detalhes
- **Critérios de entrada**: O candle completo anterior fecha acima do canal superior atrasado (para comprados) ou abaixo do canal inferior atrasado (para vendidos), enquanto a barra atual permanece de volta dentro do canal para evitar duplos gatilhos.
- **Comprado/Vendido**: Lógica simétrica para operações compradas e vendidas com flags de habilitação independentes.
- **Critérios de saída**: Rompimentos CandleStop de cor oposta fecham posições existentes; a saída opcional baseada em tempo fecha operações que permanecem abertas além do número configurado de minutos.
- **Stops**: Usa níveis de stop-loss e take-profit baseados em passo de câmbio via `StartProtection`.
- **Valores padrão**:
  - `OrderVolume` = 1
  - `UpTrailPeriods` = 5, `UpTrailShift` = 5
  - `DownTrailPeriods` = 5, `DownTrailShift` = 5
  - `SignalBar` = 1
  - `StopLossPoints` = 1000, `TakeProfitPoints` = 2000
  - `MaxPositionMinutes` = 1920
  - `CandleType` = período de 8 horas
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Canais CandleStop atrasados
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Multi-hora
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Parâmetros
- `OrderVolume`: Quantidade para cada entrada de mercado quando uma nova posição é aberta.
- `EnableLongEntry` / `EnableShortEntry`: Alternadores que permitem desabilitar novas posições compradas ou vendidas de forma independente.
- `CloseLongOnBearishBreak` / `CloseShortOnBullishBreak`: Se fechar posições existentes quando a cor de rompimento CandleStop oposta aparecer.
- `EnableTimeExit`: Ativa o filtro de tempo máximo de manutenção.
- `MaxPositionMinutes`: Número de minutos antes que uma operação aberta seja forçosamente fechada; definir como zero para desabilitar mesmo quando `EnableTimeExit` for verdadeiro.
- `UpTrailPeriods` e `UpTrailShift`: Comprimento de lookback e deslocamento para trás para o canal CandleStop altista. O deslocamento atrasa a banda estilo Donchian em várias barras para emular o timing do indicador original.
- `DownTrailPeriods` e `DownTrailShift`: Parâmetros equivalentes para o canal baixista.
- `SignalBar`: Índice da barra inspecionada para cor de rompimento (1 = candle completo anterior). A próxima barra mais antiga é usada como confirmação, assim como na versão MQL.
- `StopLossPoints` / `TakeProfitPoints`: Distâncias de stop de proteção expressas em passos de preço. Passadas para `StartProtection` para gerenciar automaticamente as saídas.
- `CandleType`: Série de candles primária usada para a estratégia. Padrão de 8 horas para corresponder ao script fonte.

## Notas de implementação
- Os valores do canal são calculados com indicadores `Highest` e `Lowest` combinados com `Shift` para reproduzir as bandas atrasadas do indicador CandleStop original.
- Cores de sinal são armazenadas em um buffer circular para imitar as chamadas `CopyBuffer` da estratégia MQL e evitar entradas duplicadas em candles consecutivos.
- Antes de colocar ordens, a estratégia verifica saídas baseadas em tempo, fecha posições opostas se necessário, e então emite novas ordens de mercado usando o volume configurado.
