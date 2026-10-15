# Estratégia Money Rain
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do consultor especialista original **MoneyRain (edição de barabashkakvn)** de MQL5 para a API de alto nível do StockSharp.
- Usa o oscilador DeMarker para escolher a direção: valores acima de 0.5 ativam entradas compradas, enquanto valores em ou abaixo de 0.5 ativam entradas vendidas.
- Opera apenas uma posição por vez e depende de offsets fixos de stop-loss/take-profit expressos em pontos.

## Dados de mercado e indicadores
- Assina o `CandleType` configurável (padrão: período de 30 minutos).
- Calcula um único indicador `DeMarker` com `DeMarkerPeriod` ajustável (padrão: 31).
- Assina cotações de Nível 1 para aproximar o spread atual, necessário pela lógica de dimensionamento adaptativo de posições.

## Lógica de trading
1. Processa apenas velas finalizadas para permanecer alinhado com a lógica original de "nova barra" (verificação `iTime(0)` em MQL).
2. Enquanto uma posição existe, monitora o máximo/mínimo da vela contra os níveis de stop-loss e take-profit pré-calculados. Se um deles for tocado, fecha a posição com uma ordem a mercado e marca o resultado como perda ou lucro.
3. Quando não há posição aberta e o limite de perdas não foi atingido, calcula o volume da operação.
4. Entra comprado em `DeMarker > 0.5`; caso contrário entra vendido. A estratégia cancela quaisquer ordens pendentes antes de enviar a ordem a mercado.

## Gestão de capital
- Reproduz a lógica `getLots()` da versão MQL rastreando:
  - `_lossesVolume`: volume acumulado de operações perdedoras recentes escalado pelo tamanho de lote base.
  - `_consecutiveLosses` e `_consecutiveProfits`: contadores de sequências usados para decidir quando reiniciar o acumulador de perdas.
- Quando a primeira operação lucrativa aparece após uma sequência perdedora (`_consecutiveProfits == 0`), o próximo tamanho de ordem é aumentado de acordo com a fórmula original:
  \[
  \text{volume} = \text{BaseVolume} \times \frac{_lossesVolume \times (\text{StopLossPoints} + \text{spread})}{\text{TakeProfitPoints} - \text{spread}}
  \]
- O spread é estimado a partir das melhores cotizações de compra/venda (em pontos) e é ignorado quando os dados de Nível 1 ainda não estão disponíveis.
- Configurar `FastOptimize = true` desabilita o dimensionamento adaptativo e sempre usa o lote base.

## Controles de risco
- `StopLossPoints` e `TakeProfitPoints` são convertidos a preços absolutos usando o passo de preço do instrumento com um multiplicador adicional de 10x para símbolos de 3 ou 5 dígitos (reflete a lógica `digits_adjust` do MQL).
- `LossLimit` bloqueia mais operações assim que o número de perdas consecutivas excede o limite definido pelo usuário (padrão: praticamente desabilitado em 1.000.000).

## Parâmetros
| Parâmetro | Descrição | Padrão |
| --- | --- | --- |
| `DeMarkerPeriod` | Período de média do indicador DeMarker. | 31 |
| `TakeProfitPoints` | Offset de take-profit em pontos estilo DeMarker. | 5 |
| `StopLossPoints` | Offset de stop-loss em pontos estilo DeMarker. | 20 |
| `BaseVolume` | Volume de ordem padrão (tamanho de lote). | 0.01 |
| `LossLimit` | Máximas perdas consecutivas permitidas antes de pausar. | 1.000.000 |
| `FastOptimize` | Quando `true`, desabilita o dimensionamento adaptativo de posições. | `false` |
| `CandleType` | Tipo de dados de velas usado para cálculos. | Velas de 30 minutos |

## Notas de implementação
- Os stops e alvos são emulados verificando os extremos das velas. A ordem de execução intrabar não pode ser recuperada, portanto toques simultâneos favorecem o ramo do stop-loss (suposição conservadora).
- `OnOwnTradeReceived` é usado para detectar quando uma ordem de saída protetora foi concluída, permitindo à estratégia atualizar os contadores de sequência e o acumulador de volume de perdas.
- O código mantém indentação com tabulações e usa comentários em inglês, seguindo as diretrizes do repositório.

## Arquivos
- `CS/MoneyRainStrategy.cs` – implementação da estratégia.
- `README.md` / `README_ru.md` / `README_zh.md` – documentação multilíngue.

## Diferenças em relação à versão MQL
- As ordens protetoras do lado do corretor são substituídas por saídas a mercado baseadas nos intervalos das velas.
- O spread é aproximado a partir de cotações de Nível 1 em vez de diretamente dos metadados do símbolo.
- A funcionalidade de e-mail e as verificações explícitas de `IsTradeAllowed` são omitidas porque o ambiente StockSharp gerencia a conectividade separadamente.
