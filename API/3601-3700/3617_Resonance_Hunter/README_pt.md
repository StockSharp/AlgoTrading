# Estratégia de Caçador de Ressonância
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Resonance Hunter é a porta StockSharp do MetaTrader consultor especialista `Exp_ResonanceHunter`. Ele monitora três pares de moedas correlacionados por slot e procura impulso síncrono em seus osciladores Stochastic. Quando os osciladores ressoam na mesma direção, a estratégia abre uma posição no símbolo primário, enquanto os símbolos secundário e de confirmação atuam como filtros. A negociação é fechada assim que o instrumento líder perde impulso ou quando o stop loss configurado é atingido.

Três slots estão pré-configurados:

1. EURUSD foi negociado com EURJPY e USDJPY como confirmações.
2. GBPUSD foi negociado com GBPJPY e USDJPY.
3. AUDUSD foi negociado com AUDJPY e USDJPY.

Cada slot pode ser ativado ou desativado de forma independente e pode usar seu próprio período de tempo e parâmetros de indicador.

## Parâmetros
Todos os parâmetros são agrupados por slot (Slot 1–3). Cada grupo compartilha as seguintes configurações:

| Parâmetro | Descrição |
| --- | --- |
| `{Slot} Enabled` | Permite a negociação do slot. |
| `{Slot} Primary` | Instrumento negociado pela estratégia e utilizado para sinais de saída. |
| `{Slot} Secondary` | Segundo instrumento que participa da verificação de ressonância. |
| `{Slot} Confirmation` | Terceiro instrumento utilizado na verificação de ressonância. |
| `{Slot} Candle Type` | Prazo aplicado a todos os três instrumentos (padrão = 1 hora). |
| `{Slot} K Period` | Stochastic %K retrospectiva. |
| `{Slot} D Period` | Período de suavização para %D. |
| `{Slot} Slowing` | Suavização adicional para %K. |
| `{Slot} Volume` | Volume do pedido em lotes. A exposição oposta existente é compensada. |
| `{Slot} Stop Loss` | Distância de stop-loss estilo MetaTrader em pontos. Defina como 0 para desabilitar a parada de proteção. |

## Lógica de negociação
1. Para cada instrumento configurado, um `StochasticOscillator` com os parâmetros selecionados é calculado nas velas concluídas.
2. Uma vez que as últimas velas dos três instrumentos compartilham o mesmo tempo de abertura, as diferenças `%K - %D` são avaliadas:
   * A diferença positiva marca um impulso ascendente (`Up`), a diferença negativa marca um impulso descendente (`Down`).
   * Regras de consistência adicionais do indicador original ajustam os impulsos comparando a magnitude de cada par.
3. Uma **entrada longa** é gerada quando todos os três impulsos apontam para cima. Uma **entrada curta** aparece quando todos os três impulsos apontam para baixo.
4. Antes de enviar novas ordens, a estratégia fecha as posições existentes se o símbolo primário indicar um impulso oposto (espelha os buffers `UpStop`/`DnStop` do indicador).
5. Depois de inserir uma posição, um preço de stop de proteção é calculado usando o último fechamento e a distância `{Slot} Stop Loss`. A cada nova vela primária, o stop é verificado e, se violado, a posição é fechada imediatamente.

Os pedidos são encaminhados por meio de `BuyMarket`/`SellMarket`. A exposição existente no símbolo primário é compensada para que a estratégia possa reverter diretamente quando necessário.

## Notas
* A estratégia requer dados de velas sincronizados para os três instrumentos dentro de cada slot. Se um símbolo ficar para trás, o sinal será adiado até que os carimbos de data e hora da barra se alinhem.
* Os níveis de stop são emulados dentro da estratégia (nenhuma ordem de stop real é enviada) para corresponder ao comportamento MetaTrader.
* Os valores de parâmetro padrão reproduzem o consultor especialista original, mas podem ser otimizados por meio da interface `Param`.
