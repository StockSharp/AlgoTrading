# Estratégia de Rompimento The 20s
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é uma conversão em C# do consultor especialista MetaTrader **Exp_The_20s_v020**. Reproduz a lógica do indicador original "The 20s" que busca padrões de rompimento após uma compressão de volatilidade. O algoritmo analisa candles completados de um período configurável e reage quando o preço rompe as bandas de 20% ao redor da faixa da barra anterior. A implementação mantém o caráter de alto nível da API do StockSharp e expõe todas as permissões de negociação para que você possa habilitar ou desabilitar ações compradas ou vendidas de forma independente.

## Lógica de sinal
O indicador monitora os candles mais recentes e calcula níveis de referência a partir da barra anterior:

1. Medir a faixa do candle anterior: `range = high[1] - low[1]`.
2. Construir dois limiares ao redor dessa barra:
   - `top = high[1] - range * Ratio`
   - `bottom = low[1] + range * Ratio`
3. Comparar o candle atual com os limiares e a distância `LevelPoints` (convertida para preço usando o `PriceStep` do instrumento).

O código original expõe dois modos de cálculo:

- **Mode1 (padrão)** – busca um falso rompimento dentro da banda de 20% no candle anterior seguido de uma forte rejeição no candle atual. Dependendo de `IsDirect`, a estratégia compra a queda (`Direct = true`) ou a vende (`Direct = false`).
- **Mode2** – requer uma série de três candles em expansão antes do sinal. Se a compressão estoura para baixo e o preço abre abaixo da banda inferior, uma direção é acionada; se abre acima da banda superior, a direção oposta é acionada. `IsDirect` novamente inverte a direção para corresponder ao comportamento do EA original.

O parâmetro `SignalBar` adia a execução em vários barras (0 = candle atual, 1 = candle anterior, etc.). Isso reproduz a capacidade do consultor especialista de agir em sinais mais antigos uma vez que estejam completamente formados.

## Gestão de negociações
- **Entradas**: `AllowLongEntry` e `AllowShortEntry` controlam se novas posições são abertas. O parâmetro `OrderVolume` define o tamanho da negociação para qualquer nova posição.
- **Reversões de posição**: Quando um sinal altista aparece, a estratégia primeiro cobre qualquer exposição vendida (`AllowShortExit`) e então opcionalmente abre uma posição comprada. O sinal baixista espelha esta lógica para posições compradas.
- **Stops e alvos**: `StopLossPoints` e `TakeProfitPoints` são medidos em pontos do instrumento. São convertidos para preços usando `PriceStep` e avaliados em cada candle completado. Se algum nível for tocado, a posição é fechada imediatamente.
- **Modo direto**: Definir `IsDirect` como `true` imita as saídas do indicador original. Mudar para `false` inverte as direções das setas, o que é útil quando você quer espelhar o comportamento em mercados com características diferentes.

## Parâmetros
- `OrderVolume` – padrão `1`. Tamanho do lote para novas posições.
- `StopLossPoints` – padrão `1000`. Stop protetor em pontos (`0` o desabilita).
- `TakeProfitPoints` – padrão `2000`. Alvo de lucro em pontos (`0` o desabilita).
- `AllowLongEntry` / `AllowShortEntry` – habilitar entradas compradas/vendidas.
- `AllowLongExit` / `AllowShortExit` – permitir à estratégia fechar posições existentes quando sinais opostos ocorrerem.
- `SignalBar` – padrão `1`. Número de barras a aguardar antes de agir em um sinal.
- `LevelPoints` – padrão `100`. Distância que confirma rompimentos além dos extremos da barra anterior.
- `Ratio` – padrão `0.2`. Largura das bandas de 20% ao redor do candle anterior.
- `IsDirect` – padrão `false`. Mantém o mapeamento original de compra/venda quando `true`, inverte quando `false`.
- `Mode` – padrão `Mode1`. Seleciona entre os dois algoritmos de cálculo.
- `CandleType` – padrão período `H1`. Define a assinatura usada para os cálculos.

## Notas
- A estratégia funciona apenas com candles completados; candles parciais são ignorados para evitar negociações prematuras.
- Todas as entradas de log e comentários em linha estão em inglês para manter o código consistente com as amostras do StockSharp.
- O gerenciamento de stop e alvo é tratado dentro da estratégia e não depende de ordens adicionais, o que torna o comportamento portátil entre simuladores e corretores ao vivo.
- Você pode anexar a estratégia a qualquer instrumento. Apenas certifique-se de que a propriedade `PriceStep` esteja disponível para que as distâncias baseadas em pontos sejam convertidas corretamente.
- Considere combinar `Mode2` com um `SignalBar` maior em períodos mais altos para emular o comportamento de "aguardar confirmação" do EA.
