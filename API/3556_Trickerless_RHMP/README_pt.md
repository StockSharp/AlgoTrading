# Estratégia RHMP sem truques (StockSharp Porta)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia transporta o consultor especialista MetaTrader **Trickerless RHMP** para o StockSharp de alto nível de API. Ele mantém o multi-estágio
lógica de entrada do robô original – combinando confirmação do Índice Direcional Médio, estrutura de média móvel suavizada e
gerenciamento de posição orientado à volatilidade – seguindo as convenções da estrutura documentadas em `AGENTS.md`.

## Lógica de negociação

1. **Indicadores**
   - Average True Range (ATR) com período configurável para dimensionamento de volatilidade.
   - Índice direcional médio (ADX) com componentes +DI/-DI completos para qualificar a força da tendência.
   - Duas médias móveis suavizadas (SMMA) representando os filtros de tendência rápida e lenta.

2. **Avaliação de tendências**
   - A inclinação lenta do SMMA deve estar dentro do corredor `MinSlopePips`…`MaxSlopePips` (medido em pips de instrumento).
   - ADX deve exceder `AdxThreshold` e subir em comparação com a vela anterior.
   - O preço deve ficar pelo menos `TrendSpacePips` longe do SMMA rápido para evitar congestionamento.
   - Uma tendência de alta requer o SMMA rápido acima do SMMA lento, +DI ≥ -DI e uma média rápida ascendente. A tendência de baixa reflete isso
verificações.

3. **Entradas principais**
   - Quando o viés de alta (ou baixa) está ativo, a estratégia abre uma ordem longa (ou curta) com volume `OrderVolume`, respeitando
`MaxNetPositions` e aguardando pelo menos `SleepInterval` entre as entradas.
   - Se existir uma posição líquida oposta, ela será nivelada primeiro para manter a cobertura desativada.

4. **Entradas de pico**
   - Se o intervalo da vela atual exceder `CandleSpikeMultiplier` vezes o intervalo anterior, a estratégia pode disparar um auxiliar
posição na direção do corpo da vela quando os componentes ADX concordam. A posição usa `OrderVolume * SpikeVolumeMultiplier`.

## Gestão de risco

- Stop-loss baseado em ATR, take-profit e trailing stop opcional (`StopLossAtrMultiplier`, `TakeProfitAtrMultiplier`, `TrailingAtrMultiplier`).
- Proteção em toda a sessão: quando o PnL alcançado atingir `DailyProfitTarget` (fração do patrimônio inicial), novas entradas serão bloqueadas.
- O interruptor de emergência global `EmergencyExit` fecha todas as posições imediatamente quando alternado.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `CandleType` | Prazo usado para todos os cálculos. | Velas de 5 minutos |
| `OrderVolume` | Volume base para cada entrada. | 0,03 |
| `AtrPeriod` | ATR comprimento de lookback. | 14 |
| `AdxPeriod` | ADX comprimento de lookback. | 14 |
| `AdxThreshold` | Valor mínimo de ADX para permitir a negociação. | 10 |
| `FastMaPeriod` | Período de média móvel suavizada rápida. | 60 |
| `SlowMaPeriod` | Período de média móvel suavizada lenta. | 120 |
| `MinSlopePips` / `MaxSlopePips` | Corredor de declive permitido para o SMMA lento. | 2/9 |
| `TrendSpacePips` | Distância mínima de preço do SMMA rápido (em pips). | 5 |
| `CandleSpikeMultiplier` | Quanto maior o intervalo da vela deve ser para acionar entradas de pico. | 7 |
| `TakeProfitAtrMultiplier` | ATR múltiplos para obtenção de lucro. | 1,0 |
| `StopLossAtrMultiplier` | ATR múltiplos para stop-loss. | 1,5 |
| `TrailingAtrMultiplier` | ATR múltiplos para trailing-stop (0 desabilita). | 0 |
| `MaxNetPositions` | Número máximo de unidades de posição líquida simultâneas. | 1 |
| `SleepInterval` | Tempo mínimo entre entradas consecutivas. | 24 minutos |
| `DailyProfitTarget` | Fração do patrimônio inicial que bloqueia a negociação quando alcançada. | 0,045 |
| `AllowNewEntries` | Chave mestre para ativar/desativar entradas. | verdade |
| `SpikeVolumeMultiplier` | Multiplicador de volume para entradas de pico. | 1,0 |
| `EmergencyExit` | Fecha todas as posições imediatamente quando verdadeiro. | falso |

## Notas

- A porta StockSharp concentra-se no alto nível limpo API em vez do microgerenciamento ticket por ticket de MetaTrader. Todos
a lógica de gerenciamento de dinheiro é implementada por meio de níveis baseados em `Volume` e ATR.
- O EA original teve várias verificações de saldo e margem. Eles são aproximados com `DailyProfitTarget`, `MaxNetPositions`
e ATR parâmetros de dimensionamento para que o comportamento permaneça alinhado sem chamadas diretas de conta MT4.
- Como a estratégia utiliza médias suavizadas, certifique-se de que haja um período de aquecimento suficiente antes de avaliar as negociações.
