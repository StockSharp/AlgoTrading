# Martingale Estratégia de simulador comercial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

`MartingaleTradeSimulatorStrategy` recria o consultor especialista "Martingale Trade Simulator" de MetaTrader dentro da estrutura StockSharp. A estratégia é um painel de negociação manual que permite ao trader enviar ordens de mercado imediatas, aplicar a média no estilo martingale e gerenciar a proteção de rastreamento sem scripts de automação adicional. Ele reage às mudanças de parâmetros em tempo real, tornando-o adequado para experimentos do Strategy Tester, assim como o robô MQL original.

## Como funciona

### Botões manuais de mercado
- Os parâmetros `Buy` e `Sell` atuam como botões virtuais. Quando qualquer parâmetro é definido como `true`, a estratégia envia uma ordem de mercado com volume `Order Volume` e então redefine automaticamente o parâmetro para `false`.
- Nenhuma ordem pendente é usada — a estratégia funciona inteiramente com execuções de mercado, refletindo o comportamento do simulador dentro do testador visual de MetaTrader.

### Média de Martingale
- Ativar `Enable Martingale` permite que o painel faça pedidos de média quando o parâmetro `Martingale` é alternado para `true`.
- A estratégia verifica a posição ativa:
  - **Posição longa:** Se o preço de venda atual estiver pelo menos `Martingale Step (points)` abaixo do preço de compra mais baixo preenchido, uma nova ordem de compra será enviada.
  - **Posição curta:** Se o preço de compra atual estiver pelo menos `Martingale Step (points)` acima do preço de venda preenchido mais alto, uma nova ordem de venda será emitida.
- Cada volume médio de pedidos é igual a `Order Volume × Martingale Multiplier^N`, onde `N` é o número de entradas consecutivas na direção atual.
- Quando o martingale está ativo, a meta de lucro é recalculada para o preço de entrada médio ponderado mais/menos `Martingale TP Offset (points)` para cobrir o rebaixamento acumulado.

### Módulo de parada móvel
- `Enable Trailing` ativa um trailing stop de proteção que segue o melhor preço mais recente.
- O trailing stop começa a `Trailing Stop (points)` de distância do preço de mercado e avança somente depois que o preço melhora em pelo menos `Trailing Step (points)`.
- Se o preço de mercado ultrapassar o nível final, a estratégia fecha imediatamente toda a posição com uma ordem de mercado oposta.

### Stop-loss e take-profit
- `Stop Loss (points)` e `Take Profit (points)` reproduzem os controles básicos de risco do consultor especialista original.
- Para posições longas, o stop é colocado abaixo do preço médio de entrada, enquanto o take-profit fica acima. Para posições curtas, ambos os níveis são espelhados.
- As saídas protetoras são executadas com ordens de mercado, portanto a estratégia permanece compatível com qualquer conector suportado por StockSharp.

## Parâmetros

| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `Order Volume` | Tamanho base para ordens manuais de mercado. | `1` |
| `Stop Loss (points)` | Distância até a parada de proteção. Zero desativa o stop loss. | `500` |
| `Take Profit (points)` | Distância até o alvo protetor. Zero desativa o take-profit. | `500` |
| `Enable Trailing` | Liga/desliga o módulo de parada móvel. | `true` |
| `Trailing Stop (points)` | Distância entre o preço e o trailing stop. | `50` |
| `Trailing Step (points)` | Movimento favorável mínimo necessário para avançar o trailing stop. | `20` |
| `Enable Martingale` | Permite calcular a média de pedidos controlados pelo botão `Martingale`. | `true` |
| `Martingale Multiplier` | Multiplicador de volume usado para cada negociação média adicional. | `1.2` |
| `Martingale Step (points)` | Movimento adverso necessário antes que uma ordem de média seja permitida. | `150` |
| `Martingale TP Offset (points)` | Compensação adicional aplicada ao nível médio de take-profit. | `50` |
| `Buy` | Defina como `true` para enviar uma ordem de compra de mercado (redefinições automáticas). | `false` |
| `Sell` | Defina como `true` para enviar uma ordem de venda a mercado (redefinições automáticas). | `false` |
| `Martingale` | Defina como `true` para avaliar e colocar uma ordem média (redefinições automáticas). | `false` |

## Dicas de uso

1. Anexe a estratégia a um instrumento, defina `Order Volume` e inicie-a no modo testador ou ao vivo.
2. Use os alternadores `Buy` / `Sell` para simular cliques de botão no painel MetaTrader.
3. Após a primeira negociação, acione a alternância `Martingale` sempre que o preço se mover contra a posição. A estratégia verifica a distância do preço e aumenta o volume se as condições forem atendidas.
4. Ajuste os parâmetros de trilha e risco para replicar o comportamento do EA original ou para experimentar configurações alternativas.

## Notas

- A estratégia depende dos dados do Nível 1 (melhor oferta/venda e última negociação) para avaliar as condições do mercado.
- Todos os comentários dentro do código C# estão em inglês, mantendo a consistência com as diretrizes do repositório.
