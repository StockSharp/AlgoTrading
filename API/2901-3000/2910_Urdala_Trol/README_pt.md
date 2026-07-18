# Estratégia de Grade de Cobertura Urdala Trol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia de Grade de Cobertura Urdala Trol** é uma conversão direta do consultor especializado MetaTrader 5 `Urdala_Trol.mq5` para a API de alto nível do StockSharp. A estratégia mantém continuamente exposição em ambas as direções e escala posições usando uma grade do tipo martingale quando os stops são atingidos. Opera inteiramente com dados Level1 (melhor bid/ask) sem nenhum indicador.

## Lógica de negociação
1. **Cobertura inicial (Passo 0)** – quando não há posições ativas, a estratégia abre imediatamente uma ordem de mercado comprada e uma vendida usando o parâmetro *Base Volume*.
2. **Escalamento do lado perdedor (Passo 1.2)** – se apenas uma direção permanece aberta e a posição mais perdedora nesse lado está pelo menos `Grid Step` pips do preço atual, a estratégia abre uma posição adicional na mesma direção. O novo volume é igual ao volume da posição menos rentável mais `Min Lots Multiplier * minVolumeStep`, onde `minVolumeStep` é derivado do `VolumeStep` ou `MinVolume` do instrumento.
3. **Tratamento do stop-loss (Passo 1.1)** – quando uma posição é fechada pelo stop-loss (incluindo ajustes de trailing) com resultado negativo, a estratégia reentra na mesma direção, a menos que já haja uma operação ativa a menos de `Min Nearest` pips do preço de saída.
4. **Reação ao stop lucrativo (Passo 2.1)** – quando o stop fecha uma posição com lucro, a estratégia abre imediatamente uma operação na direção oposta com o volume escalado.
5. **Trailing stop** – uma vez que o preço avança `Trailing Stop + Trailing Step` pips além da entrada, o stop é ajustado para manter uma distância de `Trailing Stop` pips. O trailing é opcional e aplicado apenas quando ambos os parâmetros são maiores que zero.

Todas as distâncias expressas em pips são convertidas em deslocamentos de preço absolutos através do `PriceStep` do instrumento. Para cotações de cinco ou três dígitos, a conversão multiplica o passo por dez para corresponder à lógica "adjusted point" do MQL original.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `BaseVolume` | 0.1 | Tamanho de lote inicial usado para abrir o primeiro par de cobertura. |
| `MinLotsMultiplier` | 3 | Número de lotes mínimos adicionados ao volume da operação perdedora ao escalar. |
| `StopLossPips` | 50 | Distância do stop-loss em pips. Um valor de zero desativa o stop e a lógica de trailing. |
| `TrailingStopPips` | 5 | Distância do trailing stop em pips. Definir como zero para desativar o trailing. |
| `TrailingStepPips` | 5 | Distância adicional em pips necessária antes do trailing stop se mover. Deve ser positivo quando o trailing está habilitado. |
| `GridStepPips` | 50 | Distância mínima de preço (em pips) entre a posição perdedora e o preço atual antes de uma nova ordem de escalamento ser colocada. |
| `MinNearestPips` | 3 | Se uma posição existente estiver mais próxima que esta distância ao último preço de stop, a estratégia ignora a reentrada imediata. |

## Notas de implementação
- Usa `SubscribeLevel1()` para rastrear atualizações de bid/ask e executar o motor de decisão em cada tick.
- As ordens são registradas via helper de alto nível `RegisterOrder`, permitindo rastreamento preciso através de `OnOwnTradeReceived`.
- Objetos de posição individuais são gerenciados internamente para reproduzir o comportamento de cobertura, pois os portfólios do StockSharp são baseados em posição líquida por padrão.
- A lógica de stop-loss e trailing é executada dentro da estratégia enviando ordens de mercado quando os limites são ultrapassados; nenhuma ordem stop nativa é registrada.

## Dicas de uso
1. Atribua um instrumento líquido e um portfólio à estratégia e certifique-se de que `PriceStep`, `VolumeStep` e os valores mínimos/máximos de volume estão configurados para conversões precisas.
2. Inicie a estratégia; ela construirá instantaneamente um par coberto e depois reagirá a eventos de stop de acordo com a lógica MQL original.
3. Ajuste os parâmetros de pips para alinhar com a volatilidade do instrumento. Valores grandes de `Grid Step` reduzem a frequência de ordens adicionais, enquanto um `Min Lots Multiplier` maior acelera o crescimento martingale.
4. Monitore a exposição resultante com cuidado; o comportamento martingale pode escalar o volume rapidamente quando múltiplos stops são atingidos consecutivamente.

A implementação em Python não é fornecida intencionalmente nesta pasta, conforme os requisitos desta tarefa de conversão.
