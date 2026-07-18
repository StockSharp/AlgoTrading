# Estratégia de dia nublado de Raymond
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Raymond Cloudy Day é uma estratégia de acompanhamento de breakout que reconstrói a lógica de negociação do consultor especialista **"Raymond Cloudy Day for EA"** MQL5 original. O algoritmo deriva um conjunto de níveis de referência de uma vela de prazo mais alto e os utiliza para detectar a retomada do impulso no prazo de execução. A porta StockSharp mantém as regras de negociação originais enquanto expõe cada componente como parâmetros de estratégia configuráveis.

## Dados de mercado
- **Velas de sinal** – o período em que as negociações são executadas. A estratégia subscreve esta série para sinais de entrada e gestão de posições.
- **Velas dinâmicas** – o período de tempo mais alto usado para calcular os níveis de Raymond. Por padrão, esta é uma vela diária, reproduzindo a entrada MQL5 `RayMondTimeframe`.

Ambas as assinaturas são registradas automaticamente por meio de `GetWorkingSecurities`, portanto, a estratégia solicita os fluxos de dados necessários assim que é iniciada.

## Cálculo do nível Raymond
Para cada vela pivô finalizada, a estratégia armazena os quatro níveis principais definidos pelo EA original:

\[
\begin{alinhado}
TradeSS &= \frac{Alta + Baixa + Abertura + Fechamento}{4} \\
PivotRange &= Alto - Baixo \\
ETB &= TradeSS + 0,382 \vezes PivotRange \\
ETS &= TradeSS - 0,382 \vezes PivotRange \\
TPB1 &= TradeSS + 0,618 \vezes PivotRange \\
TPS1 &= TradeSS - 0,618 \vezes PivotRange \\
TPB2 &= TradeSS + PivotRange \\
TPS2 &= TradeSS - PivotRange
\end{alinhado}
\]

A implementação StockSharp mantém o instantâneo mais recente desses valores e registra cada atualização, permitindo ao usuário monitorar como os níveis evoluem ao longo do tempo.

## Lógica de entrada
Assim que os níveis de Raymond estiverem disponíveis, a estratégia avalia cada vela de sinalização finalizada:

1. **Configuração longa** – Se a mínima da vela cair abaixo de `TPS1` e o fechamento retornar acima do nível, a estratégia entra em uma posição longa. Isso reflete a condição EA `Low[1] < TPS1 && Close[1] > TPS1` e captura a rejeição de alta do nível.
2. **Configuração curta** – Se a vela permanecer totalmente acima de `TPS1`, mas fechar abaixo dela, a estratégia abre uma posição curta (correspondendo à regra original, embora assimétrica).

Antes de colocar uma nova ordem, o algoritmo cancela quaisquer ordens pendentes e, se necessário, fecha a posição oposta para que apenas uma negociação direcional permaneça ativa.

## Gestão de risco
Raymond Cloudy Day usa compensações de proteção simétricas medidas em ticks:

- **Stop-loss** – posicionado `ProtectiveOffsetTicks` abaixo da entrada longa (ou acima da entrada curta).
- **Take-profit** – posicionado `ProtectiveOffsetTicks` acima da entrada longa (ou abaixo da entrada curta).

As compensações são multiplicadas pelo `PriceStep` do instrumento para converter ticks em distâncias de preço absolutas. Cada vela de sinal concluída aciona uma verificação que fecha a posição quando qualquer nível de proteção é atingido. Quando a estratégia é plana, o estado de proteção interna é redefinido para evitar níveis obsoletos.

## Parâmetros
| Nome | Descrição | Padrão | Notas |
|------|-------------|---------|-------|
| `TradeVolume` | Volume de pedidos usado para cada entrada. | `1` | Sincronizado com a propriedade `Volume` na inicialização. |
| `ProtectiveOffsetTicks` | Distância em ticks para stop-loss e take-profit. | `500` | Multiplicado por `PriceStep` para obter preços absolutos. |
| `SignalCandleType` | Tipo de vela que produz sinais de negociação. | `1 hour` período de tempo | Pode ser definido como qualquer `DataType` representando velas. |
| `PivotCandleType` | Prazo maior para cálculos de nível Raymond. | `1 day` período de tempo | Corresponde à entrada `RayMondTimeframe` do MQL EA. |

Todos os parâmetros suportam intervalos de otimização e metadados descritivos para o StockSharp Designer.

## Notas adicionais
- A estratégia requer que `PriceStep` seja definido pela segurança conectada. Se estiver faltando, as entradas comerciais serão ignoradas e um aviso será registrado.
- A visualização do gráfico adiciona as velas de execução às negociações executadas. Desenho personalizado adicional pode ser adicionado, se desejado.
- A implementação evita a pesquisa direta de valor do indicador e processa apenas velas concluídas, aderindo às diretrizes do projeto em `AGENTS.md`.

## Especificações originais do EA preservadas
- Fórmulas e multiplicadores de nível Raymond (`0.382`, `0.618`, `1.0`).
- Lógica de entrada baseada no take-profit da primeira venda (`TPS1`).
- Compensações simétricas de stop-loss e take-profit de 500 pontos convertidas em ticks no ambiente StockSharp.

Com esses componentes, a estratégia StockSharp se comporta de forma idêntica à fonte EA, ao mesmo tempo que fornece configuração avançada e registro adequado para pesquisas e automação adicionais.
