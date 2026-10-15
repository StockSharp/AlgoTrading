# Estratégia de Rompimento de Vela Anterior 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia de rompimento que replica o especialista MetaTrader "Previous Candle Breakdown 2". O algoritmo observa a vela mais recentemente completada em um período configurável e aciona operações quando o preço perfura seu máximo ou mínimo por um deslocamento de pips definido pelo usuário. Filtragem opcional por médias móveis, horários de negociação estritos, dimensionamento de posição por volume fixo ou risco percentual e saídas de proteção em camadas replicam o comportamento da versão MQL original dentro do StockSharp.

## Visão geral
- **Lógica de entrada**: Entrar comprado quando o preço excede o máximo da vela anterior mais um recuo. Entrar vendido quando o preço rompe abaixo do mínimo da vela anterior menos o mesmo recuo.
- **Filtros**: Médias móveis rápidas/lentas opcionais com parâmetros de deslocamento exigem confirmação direcional antes de negociar. A negociação também é limitada a uma janela de tempo de início/fim.
- **Dimensionamento de posição**: Escolher entre um volume de ordem fixo ou dimensionamento dinâmico baseado no valor do portfólio e na distância do stop-loss.
- **Controles de risco**: Níveis estáticos de stop-loss e take-profit em pips, trailing stop com filtro de passo e um objetivo de lucro global que fecha todas as posições.
- **Escala**: O limite `MaxPositions` limita o tamanho absoluto da posição líquida para cada direção.

## Valores padrão
- `IndentPips` = 10
- `FastPeriod` = 10, `FastShift` = 3, `SlowPeriod` = 30, `SlowShift` = 0, `MaMethod` = Simple
- `StopLossPips` = 50, `TakeProfitPips` = 150
- `TrailingStopPips` = 15, `TrailingStepPips` = 5
- `ProfitClose` = 100 (unidades de moeda de PnL realizado + não realizado)
- `MaxPositions` = 10 (contagem absoluta de contratos/lotes por lado)
- `OrderVolume` = 0 (desabilitado), `RiskPercent` = 5 (usado quando `OrderVolume` é zero e o stop-loss está ativo)
- `StartTime` = 09:09, `EndTime` = 19:19
- `CandleType` = período de 4 horas

## Regras de negociação
1. Inscrever-se na série de velas configurada e registrar cada vela finalizada.
2. Verificar se a hora atual está dentro da sessão de negociação permitida. Se `ProfitClose` for atingido, nivelar imediatamente.
3. Calcular os níveis de rompimento adicionando/subtraindo o recuo em pips do máximo e mínimo da vela anterior.
4. Quando o preço romper esses níveis e as condições de MA (se habilitadas) forem satisfeitas, abrir operações respeitando o limite `MaxPositions`.
5. Definir distâncias iniciais de stop-loss e take-profit a partir do preço de entrada e ativar trailing stops quando o preço tiver se movido a favor da operação pelo menos a distância de trailing mais o passo.
6. Monitorar continuamente as velas: acionar saídas de stop-loss/take-profit quando tocados, arrastar stops conforme o preço avança e redefinir níveis de proteção assim que as posições forem fechadas.

## Notas
- Os cálculos de pips se ajustam automaticamente para instrumentos de 3 ou 5 decimais para imitar a conversão de ponto para pip do MetaTrader.
- Ao usar dimensionamento de risco percentual, o algoritmo estima o volume a partir do valor atual do portfólio e do stop-loss configurado.
- A verificação de rompimento usa velas finalizadas, portanto picos dentro da barra são avaliados nos níveis de fechamento/máximo/mínimo da vela.
- `MaxPositions` trabalha com a posição líquida da estratégia. Se volumes fracionários forem usados, o parâmetro representa o tamanho líquido absoluto máximo permitido por direção.
- Os gráficos exibem velas, as médias móveis ativas quando habilitadas e as operações executadas para confirmação visual.
