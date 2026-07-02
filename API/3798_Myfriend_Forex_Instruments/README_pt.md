# Estratégia de instrumentos Forex Myfriend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de Instrumentos Forex Myfriend** reproduz o especialista "MyFriend" MetaTrader de 2006. Ele negocia EUR/USD em velas de 30 minutos, combinando níveis de pivô diários, Donchian expansões de canal e um spread de impulso curto versus longo medido a partir dos preços de fechamento. O sistema procura velas que perfurem o pivô diário com um corpo real largo ou por expansões abruptas de largura de Donchian. Quando um desses impulsos se alinha com o viés do momentum intradiário, a estratégia abre uma posição única com níveis de proteção pré-definidos.

## Lógica de negociação

1. **Mapa dinâmico diário** – A máxima, a mínima e o fechamento do dia anterior constroem a escada dinâmica clássica (`Pivot`, `R1`, `S1`). Esses níveis permanecem inalterados durante todo o pregão e definem a faixa de negociação esperada.
2. **Pulso de impulso** – Duas médias móveis simples no preço de fechamento (3 e 9 períodos) formam um spread de impulso curto/longo. O spread é multiplicado por 1000 para imitar o cálculo MetaTrader "MP" e determina se a pressão de alta ou de baixa domina.
3. **Filtros de fuga**
   - *Pivot Push*: depois que uma vela fecha no pivô com um corpo maior que 12 pontos e a próxima vela fecha na mesma direção, a estratégia sinaliza uma negociação potencial.
   - *Donchian expansão*: quando o canal Donchian de 16 períodos se alarga além da faixa `R1 - S1` e sua direção concorda com a ação do preço, o sinal também é acionado.
4. **Gerenciamento de pedidos** – Apenas uma posição é permitida por vez. As entradas longas usam a mínima da vela anterior menos um buffer como stop e um take-profit fixo de 70 pontos. As entradas curtas refletem essa lógica com o máximo anterior mais um buffer.
5. **Táticas de saída**
   - *Saída baseada no tempo*: entre a 3ª e a 4ª vela após a entrada, se a última barra fechada mover 3 pontos contra a posição, a negociação é fechada mais cedo.
   - *Trailing stop*: quando o lucro aberto excede 5 pontos e o limite Donchian continua a se mover a favor da negociação, o stop é seguido ao longo do canal mais/menos um buffer de 1 ponto.
   - *Alvos rígidos*: o preço que toca o stop calculado ou o take-profit fecha imediatamente a posição.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `BaseVolume` | Volume de pedidos usado para cada nova negociação. | `1` |
| `TakeProfitPoints` | Distância do take-profit até a entrada em MetaTrader pontos. | `70` |
| `StopLossBufferPoints` | Buffer adicional adicionado além do extremo anterior da vela para o stop protetor. | `13` |
| `ChannelPeriod` | Donchian período do canal usado para testes de expansão de largura e rastreamento. | `16` |
| `UseTrailingStop` | Ativa ou desativa o trailing stop baseado em Donchian. | `true` |
| `TrailingStartPoints` | Lucro aberto necessário (pontos) antes que o trailing stop possa ser reduzido. | `5` |
| `TrailingBufferPoints` | Buffer (pontos) aplicado ao limite Donchian ao rastrear. | `1` |
| `UseTimeClose` | Ativa a saída de rejeição de 3–4 velas. | `true` |
| `CandleType` | Tipo de vela primária (período padrão de 30 minutos). | `M30` |
| `DailyCandleType` | Tipo de vela diária usada para reconstruir os níveis de pivô. | `D1` |

## Notas

- A estratégia foi projetada para EUR/USD e velas de 30 minutos, refletindo o especialista original. Diferentes instrumentos ou prazos podem exigir ajustes de parâmetros.
- Os parâmetros baseados em pontos dependem do `PriceStep` do instrumento. Se não for fornecido pelos dados de mercado, a estratégia recorre a um incremento de preço unitário.
- Apenas velas concluídas são processadas, correspondendo ao comportamento MetaTrader do algoritmo de origem.
