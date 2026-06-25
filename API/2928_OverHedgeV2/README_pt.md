# Estratégia de Grade OverHedgeV2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia replica o consultor especialista OverHedge V2 do MetaTrader na API de alto nível do StockSharp. Constrói uma grade com hedge seguindo a direção de uma EMA rápida e uma lenta, alternando então ordens compradas e vendidas dentro de um túnel dinâmico. As posições são adicionadas de acordo com uma progressão geométrica de lotes e toda a cesta é liquidada quando o lucro não realizado agregado atinge o alvo configurado.

## Lógica de negociação

- **Filtro de tendência:** Uma EMA de 8 períodos deve divergir de uma EMA de 21 períodos em pelo menos `MinDistancePips`. O filtro decide a direção da primeira negociação em cada ciclo.
- **Túnel de grade:** A largura do túnel é igual ao spread atual multiplicado por dois mais `TunnelWidthPips` convertido em unidades de preço. Define o gatilho do lado oposto assim que o ciclo começa.
- **Alternância de ordens:** As primeiras três posições são abertas na direção da tendência. Depois o algoritmo alterna de lado para fazer hedge da exposição usando os mesmos âncoras do túnel como referência.
- **Escalonamento de lotes:** Cada ordem subsequente multiplica o volume anterior por `BaseMultiplier` começando em `StartVolume`. O tamanho é alinhado às restrições de volume do instrumento.
- **Saída do ciclo:** Quando o ganho líquido não realizado por lote do instrumento está acima de `MinProfitTargetPips` e o lucro total da cesta excede `ProfitTargetPips`, a estratégia fecha todas as posições abertas e redefine o estado.
- **Desligamento manual:** Definir `ShutdownGrid` como `true` fecha quaisquer posições restantes e evita novas ordens até que seja desativado.

## Condições de entrada

### Entradas compradas
- O filtro de tendência indica tendência de alta (`EMA_short - EMA_long > MinDistancePips`).
- O preço Ask é maior ou igual à âncora de compra atual.
- A estratégia não está em modo de desligamento e a cesta não atingiu seu alvo de lucro.

### Entradas vendidas
- O filtro de tendência indica tendência de baixa (`EMA_long - EMA_short > MinDistancePips`).
- O preço Ask é menor ou igual à âncora de venda atual.
- A flag de desligamento é falsa e o alvo de lucro da cesta ainda não foi atingido.

## Gestão de saída

- **Saída de lucro:** Quando o lucro não realizado da cesta satisfaz `ProfitTargetPips` com cada lado aberto ganhando pelo menos `MinProfitTargetPips` por lote, todas as posições são fechadas a mercado.
- **Saída de emergência:** Definir `ShutdownGrid` como `true` fecha imediatamente qualquer exposição aberta.

## Indicadores e dados

- EMA de 8 períodos (rápida) e EMA de 21 períodos (lenta) calculadas na série de velas configurada.
- A assinatura de Nível 1 é usada para rastrear o melhor bid/ask para construir o túnel e comparar as condições de entrada com os spreads em tempo real.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `StartVolume` | Volume inicial da primeira ordem em um ciclo. |
| `BaseMultiplier` | Multiplicador geométrico aplicado ao volume de cada ordem subsequente. |
| `TunnelWidthPips` | Largura adicional do túnel em pips adicionada ao dobro do spread atual. |
| `ProfitTargetPips` | Alvo de lucro da cesta medido em pips convertidos em distância de preço. |
| `MinProfitTargetPips` | Movimento mínimo favorável por lado antes que a cesta possa fechar. |
| `ShortEmaPeriod` | Período da EMA rápida usada para confirmação de direção. |
| `LongEmaPeriod` | Período da EMA lenta usada para confirmação de direção. |
| `MinDistancePips` | Separação mínima de EMA necessária para declarar uma tendência. |
| `CandleType` | Período de tempo das velas que alimentam as EMAs e o loop de negociação. |
| `ShutdownGrid` | Interruptor booleano que força a liquidação e bloqueia novas negociações. |

## Notas práticas

- O período de vela padrão é uma hora; ajuste-o para corresponder ao período de tempo usado no EA original.
- A estratégia depende de dados de melhor bid/ask; forneça cotações de Nível 1 durante o trading ao vivo ou backtesting.
- Como o StockSharp mantém uma posição líquida por instrumento, compras e vendas alternadas reduzirão ou reverterão a exposição líquida em vez de manter tickets com hedge independentes, mas a lógica da cesta ainda imita a captura de lucro pretendida.
- Sempre verifique as etapas de volume específicas do instrumento e os tamanhos de tick para que o túnel gerado e a escala de lotes correspondam ao mercado que você negocia.
