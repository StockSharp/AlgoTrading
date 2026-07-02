# Estratégia de reequilíbrio da rede
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Reequilíbrio de Grade é uma versão StockSharp de alto nível do consultor especialista "Grid" da Mission Automate. A estratégia alterna entre ciclos de grade longos e curtos e sempre mantém uma escada de ordens limitadas na direção ativa. Quando a posição agregada atinge um nível de lucro comum, o ciclo é fechado, todas as ordens pendentes são removidas e o próximo ciclo começa na direção oposta.

## Como funciona
1. **Início do ciclo** – Quando não há posições ou ordens pendentes, a estratégia abre uma posição de mercado na direção definida por `FirstTradeSide` usando `StartVolume` lotes.
2. **Colocação da grade** – Após cada ordem preenchida na direção ativa, o algoritmo coloca uma nova ordem limite a uma distância de `GridStepPoints` (convertido em preço pelo instrumento `PriceStep`). O volume do próximo pedido é igual ao volume do último pedido preenchido multiplicado por `LotMultiplier`.
3. **Take-profit baseado na média** – Para cada pedido atendido, o preço médio ponderado de entrada é recalculado. O take-profit para toda a cesta é definido como o preço médio mais/menos `TargetPoints` (também convertido por meio de `PriceStep`). Os máximos e mínimos das velas são usados ​​para modelar o comportamento do gatilho do corretor.
4. **Conclusão do ciclo** – Quando o nível de take-profit é atingido, a estratégia fecha toda a posição com uma ordem de mercado, cancela as ordens pendentes restantes, lembra a direção do ciclo finalizado e inverte a direção do próximo.

## Parâmetros
- `FirstTradeSide` – direção do primeiro ciclo (`Buy` ou `Sell`). Cada ciclo concluído muda automaticamente de direção.
- `StartVolume` – tamanho do lote da ordem de mercado inicial em cada ciclo.
- `LotMultiplier` – multiplicador aplicado ao volume de pedidos preenchidos mais recentemente ao preparar o próximo nível de grade. Valores maiores que um criam uma progressão semelhante a martingale.
- `GridStepPoints` – distância entre os níveis da grade expressa em pontos. A estratégia multiplica por `Security.PriceStep` para obter a diferença absoluta de preço.
- `TargetPoints` – distância do take-profit em relação ao preço médio ponderado de entrada, medido em pontos.
- `CandleType` – série de velas usada para monitorar extremos de preços para desencadear saídas.

## Gestão de riscos e comportamento
- Nenhum stop-loss explícito é usado; a grade continua adicionando exposição enquanto o mercado se move contra a posição.
- Apenas uma ordem pendente está ativa por vez. Quando o pedido é atendido, o próximo nível é agendado imediatamente.
- O ciclo não pode começar até que a posição e a fila pendente estejam vazias e o instrumento tenha um `PriceStep` válido.
- A conversão mantém todos os cálculos dentro da estratégia sem tocar em coleções globais ou buffers de indicadores, seguindo as regras do projeto.
- As ordens pendentes são canceladas sempre que um ciclo termina, evitando limites órfãos de ciclos anteriores.

## Notas
- Todas as configurações baseadas em pontos são convertidas em preços com `Security.PriceStep`. Se o passo for zero, a estratégia espera até que o instrumento o forneça.
- A implementação depende puramente do API de alto nível (`SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket`, `BuyLimit`, `SellLimit`) conforme necessário.
- Uma versão Python não foi incluída intencionalmente nesta tarefa.
