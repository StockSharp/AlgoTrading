# Estratégia reversa MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia reversa MA é uma conversão StockSharp do simples MetaTrader 4 consultor especialista "MA_Reverse". O robô original
monitora quanto tempo o preço do lance permanece acima ou abaixo de uma média móvel simples de 14 períodos (SMA). Depois de uma seqüência longa o suficiente em um
direção, abre uma posição apostando em uma reversão de curto prazo. A porta StockSharp mantém a mesma ideia contando o número
de velas concluídas consecutivas fechando acima ou abaixo de SMA e executando uma ordem de mercado assim que o limite configurado for
alcançado.

## Lógica de negociação
- Assine as velas do período selecionado e calcule uma média móvel simples com o período definido por `SmaPeriod`.
- Mantenha um contador inteiro (`StreakThreshold` controla o comprimento alvo) que aumenta enquanto o fechamento da vela permanece acima
a média móvel e diminui enquanto o fechamento permanece abaixo dela. Tocar na média móvel zera o contador.
- Quando o contador atingir `StreakThreshold` e o fechamento estiver pelo menos `MinimumDeviation` acima de SMA, a estratégia vende com um
ordem de mercado. A suposição é que uma excursão de alta prolongada a partir da média móvel provavelmente reverterá à média.
- Quando o contador atinge `-StreakThreshold` e o fechamento está pelo menos `MinimumDeviation` abaixo de SMA, a lógica espelha o
comportamento e abre uma posição longa.
- Após cada negociação, o contador mantém seu valor em execução, assim como a fonte EA, para que possa começar imediatamente a medir o
próxima sequência.

## Gerenciamento de pedidos
- As entradas no mercado usam o parâmetro `TradeVolume`. Se houver uma posição oposta no livro, a estratégia primeiro fecha-o e
em seguida, abre a nova negociação em uma única ordem de mercado para que as reversões correspondam ao comportamento MetaTrader.
- Um take-profit global é configurado através do ajudante `StartProtection` de StockSharp. A distância é igual a `TakeProfitPoints`
multiplicado pela etapa do preço do título, reproduzindo a meta de lucro de "30 * pontos" do código MQL. Quando o alvo é atingido o
posição é fechada com uma ordem de mercado.
- Nenhum stop-loss é implementado no especialista original e, portanto, nenhum é adicionado no porto. O controle de risco é inteiramente feito por
o take-profit e pelas configurações de gerenciamento de dinheiro do usuário.

## Parâmetros
| Parâmetro | Descrição |
|-----------|-------------|
| `TradeVolume` | Tamanho do lote usado para cada entrada no mercado. O valor também é usado para dimensionar reversões ao mudar de direção. |
| `SmaPeriod` | Número de velas utilizadas pela média móvel simples. O padrão corresponde à média móvel de 14 períodos de EA. |
| `StreakThreshold` | Número de fechamentos consecutivos que devem permanecer em um lado do SMA antes que uma ordem de reversão seja permitida. |
| `MinimumDeviation` | Distância absoluta mínima entre o fechamento e o SMA que confirma a condição de rompimento. |
| `TakeProfitPoints` | Distância de lucro expressa em etapas de preço. É multiplicado pelo `PriceStep` do instrumento para obter a compensação absoluta do preço. |
| `CandleType` | Tipo de vela (período de tempo) usado para calcular o SMA e avaliar os contadores de sequência. |

## Notas
- A lógica do contador funciona com velas finalizadas fornecidas por `SubscribeCandles`, o que torna a implementação robusta e
compatível com testes históricos. O comportamento corresponde à versão MetaTrader baseada em ticks, desde que as velas estejam boas
granulado o suficiente para capturar excursões de curto prazo.
- Como StockSharp agrega posições por padrão, múltiplas entradas consecutivas são gerenciadas como uma única posição com um único
distância flutuante de lucro. Isso equivale a fazer com que MetaTrader coloque o mesmo lucro em todos os pedidos porque o
a distância do preço médio de entrada atual permanece constante.
- A estratégia não adiciona seu próprio indicador a `Strategy.Indicators` porque a infraestrutura de ligação gerencia o indicador
vida útil automaticamente.
- Sempre valide as configurações de etapa de preço e volume para seus símbolos de corretor específicos para que o parâmetro `TakeProfitPoints`
produz o tamanho de destino absoluto desejado.
