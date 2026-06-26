# Estratégia One Two Three
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia One Two Three negocia rompimentos do oscilador Chaikin após um período prolongado de acumulação plana. Ela emula o especialista original do MetaTrader 5 combinando uma linha de acumulação/distribuição com dois EMAs, validando que a pressão do mercado permaneceu neutra por várias barras, e depois entrando em uma forte onda de momentum Chaikin. O port do StockSharp mantém o dimensionamento de lotes, gerenciamento de stops e lógica de trailing configuráveis através de parâmetros de estratégia.

## Conceito

- Construir o oscilador Chaikin como a diferença entre uma média móvel exponencial rápida e uma lenta aplicada à linha de acumulação/distribuição derivada das velas recebidas.
- Rastrear as últimas **BarsCount** leituras do oscilador e classificar as barras onde o valor absoluto de Chaikin permanece dentro de **FlatLevel**.
- Permitir a negociação apenas quando mais de **FlatPercent** por cento dessas leituras armazenadas permaneceram dentro do intervalo plano, sinalizando uma acumulação tranquila.
- Quando uma nova vela terminar, entrar na direção do impulso Chaikin se sua magnitude exceder **OpenLevel**.

## Regras de entrada

- **Comprado**: O oscilador Chaikin na vela recém-fechada é maior ou igual a **OpenLevel** e a posição líquida atual é não positiva.
- **Vendido**: O oscilador Chaikin na vela recém-fechada é menor ou igual ao **OpenLevel** negativo e a posição líquida atual é não negativa.
- As ordens são emitidas a mercado. Se a estratégia mantiver uma posição oposta, o tamanho da ordem é aumentado para neutralizar a exposição existente antes de estabelecer a nova negociação.

## Regras de saída

- Um stop-loss fixo (**StopLossPips**) e take-profit (**TakeProfitPips**) são traduzidos em offsets de preço usando o passo de preço do instrumento (1 pip = 1 passo de preço) e aplicados imediatamente após a entrada.
- Um trailing stop opcional ajusta o stop de proteção assim que o preço se move a favor da negociação em pelo menos **TrailingStopPips + TrailingStepPips**. O novo stop é colocado exatamente **TrailingStopPips** do fechamento atual enquanto requer o buffer de passo para evitar o aperto prematuro.
- Se o stop ou o alvo for tocado dentro do intervalo da vela completada, a posição é fechada a mercado.

## Gestão de risco e dinheiro

- **OrderVolume** controla a quantidade enviada com cada ordem de mercado. A estratégia adiciona ou subtrai automaticamente o tamanho da posição atual ao mudar de direção para que as reversões ocorram em uma única negociação.
- Definir qualquer um dos parâmetros baseados em pips como zero desativa esse componente (por exemplo, um take-profit zero mantém as negociações abertas até que o stop ou o sinal oposto ocorra).

## Parâmetros

- **OrderVolume** – Volume base para entradas.
- **StopLossPips** – Distância, em pips, entre o preço de entrada e o stop de proteção.
- **TakeProfitPips** – Distância, em pips, entre o preço de entrada e o alvo de lucro.
- **TrailingStopPips** – Distância, em pips, mantida entre o preço e o trailing stop. Definir como zero para desabilitar o trailing.
- **TrailingStepPips** – Ganho mínimo em pips além da distância de trailing necessário antes que o stop seja movido novamente.
- **FastLength** – Período do EMA rápido no oscilador Chaikin.
- **SlowLength** – Período do EMA lento no oscilador Chaikin.
- **FlatLevel** – Valor absoluto de Chaikin que ainda conta como comportamento de mercado plano.
- **OpenLevel** – Magnitude de Chaikin necessária para acionar uma nova negociação assim que a condição plana for satisfeita.
- **BarsCount** – Número de valores recentes de Chaikin a avaliar ao calcular a proporção plana.
- **FlatPercent** – Porcentagem mínima dos valores armazenados que devem permanecer dentro do intervalo plano para permitir a negociação.
- **CandleType** – Tipo de dados de vela ou período que alimenta os cálculos do indicador.

## Notas

- A lógica de trailing espelha o especialista do MetaTrader: se **TrailingStopPips** for diferente de zero, mantenha **TrailingStepPips** positivo para evitar um stop estagnado.
- Como as estratégias do StockSharp trabalham com o passo de preço do instrumento, as distâncias baseadas em pips assumem que um pip equivale a um passo de preço; ajuste os valores do parâmetro de acordo para instrumentos com diferentes tamanhos de tick.
- A estratégia processa apenas velas completadas e não tenta reagir dentro da barra, correspondendo ao especialista original que executa em novas aberturas de barra.
