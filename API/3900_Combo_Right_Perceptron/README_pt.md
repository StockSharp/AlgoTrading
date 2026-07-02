# Estratégia Combo Right Perceptron
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão StockSharp fiel do consultor especialista MetaTrader **Combo_Right.mq4**. Ele combina um filtro de momentum básico do Commodity Channel Index (CCI) com três perceptrons que analisam o momentum do preço de abertura em avanços de barra configuráveis. Dependendo do `PassMode`, os perceptrons podem substituir o sinal CCI e instruir o supervisor a abrir posições longas ou curtas com seus parâmetros de risco dedicados.

## Lógica de negociação

1. Assine o tipo de vela configurado e calcule o CCI nos preços de abertura. A última vela concluída fornece o preço de fechamento e os valores históricos de abertura para entradas do Perceptron.
2. Mantenha um buffer circular de preços de abertura para que os perceptrons possam acessar a abertura de `period`, `2*period`, `3*period` e `4*period` barras atrás sem depender de getters de histórico de indicadores.
3. Quando uma vela acabada chega:
   - Avalie o valor CCI. Isso atua como o sinal padrão (`> 0` = longo, `< 0` = curto) com as distâncias de proteção básicas (`TakeProfit1` / `StopLoss1`).
   - Dependendo de `PassMode`, calcule um ou vários perceptrons. Cada perceptron usa pesos derivados das entradas originais MQL (`X** - 100`) e das diferenças entre o fechamento mais recente e as aberturas históricas.
   - Se uma condição do perceptron for satisfeita, ele substitui o sinal padrão e atribui suas próprias distâncias de stop-loss/take-profit antes de qualquer pedido ser enviado.
4. Cancele as ordens de serviço, nivele a exposição oposta e abra a nova posição usando o `TradeVolume` configurado. Depois que a ordem de mercado for enviada, chame `SetTakeProfit` e `SetStopLoss` com as compensações calculadas para que as ordens de proteção reflitam o ramo perceptron ativo.

### Modos de passe

- **Pass 1** – somente o valor CCI é considerado. O sinal é proporcional ao último valor do indicador.
- **Passe 2** – se o primeiro perceptron (`Perceptron1Period`, `X12…X42`) produzir uma saída negativa, a estratégia abre imediatamente uma negociação curta com o segundo perfil de risco. Caso contrário, volta ao resultado CCI.
- **Passe 3** – se o segundo perceptron for positivo a estratégia abre uma negociação longa com o terceiro perfil de risco. Caso contrário, depende da saída CCI.
- **Passe 4** – primeiro verifique o terceiro perceptron. Um valor positivo exige que o segundo perceptron também seja positivo para permitir uma entrada longa com o perfil de risco de alta. Se o terceiro perceptron for negativo e o primeiro perceptron estiver abaixo de zero, o supervisor abre uma venda com perfil de risco de baixa. Se nenhuma ramificação for acionada, a saída CCI será usada.

Em todos os modos, a estratégia ignora os sinais até que velas suficientes sejam coletadas para alimentar o passo mais profundo do perceptron.

## Gestão de risco

Cada entrada calcula novas compensações de preço com base no símbolo `PriceStep`. Se o instrumento não fornecer um passo, a distância do ponto bruto será usada como está. `SetTakeProfit` e `SetStopLoss` recebem os deslocamentos desejados junto com a posição líquida resultante para que os colchetes de proteção permaneçam sincronizados com a exposição atual.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| --- | --- | --- | --- |
| `TakeProfit1`, `StopLoss1` | `decimal` | 50/50 | Distâncias de lucros e perdas (em pontos) quando a saída CCI é usada. |
| `CciPeriod` | `int` | 10 | Período do CCI calculado sobre preços de abertura. |
| `X12`, `X22`, `X32`, `X42` | `int` | 100 | Pesos brutos para o perceptron de baixa; a estratégia subtrai internamente 100 como no código original. |
| `TakeProfit2`, `StopLoss2` | `decimal` | 50/50 | Distâncias de risco (pontos) aplicadas quando o perceptron de baixa é acionado. |
| `Perceptron1Period` | `int` | 20 | Passo entre amostras para o perceptron de baixa (em barras). |
| `X13`, `X23`, `X33`, `X43` | `int` | 100 | Pesos brutos para o perceptron altista. |
| `TakeProfit3`, `StopLoss3` | `decimal` | 50/50 | Distâncias de risco (pontos) aplicadas quando o perceptron de alta é acionado. |
| `Perceptron2Period` | `int` | 20 | Passo entre amostras para o perceptron de alta (em barras). |
| `X14`, `X24`, `X34`, `X44` | `int` | 100 | Pesos brutos para o perceptron de confirmação usado em `PassMode = 4`. |
| `Perceptron3Period` | `int` | 20 | Passo entre as amostras para o perceptron de confirmação (em barras). |
| `PassMode` | `int` | 1 | Modo supervisor (1–4) que reproduz a lógica de ramificação do especialista MQL. |
| `TradeVolume` | `decimal` | 0,01 | Volume utilizado para novas entradas no mercado. A exposição oposta é fechada antes de entrar. |
| `CandleType` | `DataType` | M1 | Série de velas alimentando as entradas CCI e perceptron. |

## Notas

- A implementação espera intencionalmente até que todos os perceptrons tenham preços de abertura históricos suficientes antes de negociar, evitando problemas vinculados ao array que estavam implícitos em MetaTrader.
- Os valores dos indicadores nunca são recuperados através de acesso aleatório. Em vez disso, o histórico necessário é armazenado em um buffer circular compatível com as diretrizes do projeto.
- Todos os comentários e documentação são mantidos em inglês para atender aos requisitos do repositório.
