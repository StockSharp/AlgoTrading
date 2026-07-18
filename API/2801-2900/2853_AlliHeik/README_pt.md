# Estratégia Alli Heik
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia Alli Heik é uma conversão do assessor especialista MetaTrader 5 "AlliHeik". Ela opera o **Heiken Ashi Smoothed Oscillator** (HASO) originalmente publicado por mladen. O indicador constrói uma vela Heiken Ashi personalizada suavizando os preços brutos de abertura, máxima, mínima e fechamento com uma média móvel selecionável, aplica um passo de suavização adicional ao ponto médio de Heiken Ashi e, em seguida, mede a diferença barra a barra desse valor suavizado. Uma média móvel da diferença forma a linha de sinal.

As decisões de trading são tomadas no cruzamento do oscilador e da linha de sinal avaliada em velas completamente fechadas. A estratégia oferece um modo inverso opcional, a capacidade de fechar automaticamente posições opostas, tratamento estático de stop-loss/take-profit e um trailing stop que imita a lógica de passos da versão original do MetaTrader.

## Regras de trading

1. **Preparação do indicador**
   - Pré-suavizar dados OHLC com uma de SMA, EMA, SMMA ou LWMA.
   - Construir velas Heiken Ashi a partir dos dados suavizados e calcular a média de abertura/fechamento para obter um ponto médio.
   - Pós-suavizar o ponto médio e calcular o oscilador como a diferença entre valores suavizados consecutivos.
   - Suavizar o oscilador com uma média móvel configurável para criar a linha de sinal.
2. **Condições de entrada**
   - *Modo normal*: abrir uma posição **comprada** quando o oscilador cruza **abaixo** da linha de sinal, abrir uma posição **vendida** quando cruza **acima** da linha de sinal (reproduzindo exatamente a lógica MQL).
   - *Modo inverso*: trocar as condições de comprado e vendido.
   - Os sinais são avaliados apenas em velas terminadas. As posições existentes podem opcionalmente ser fechadas antes de entrar em uma nova operação na direção oposta.
3. **Gerenciamento de saídas**
   - As distâncias estáticas de stop-loss e take-profit são expressas em pips e convertidas para preço usando o tamanho de tick e os decimais do instrumento.
   - Um trailing stop torna-se ativo assim que o preço avança *TrailingStop + TrailingStep* pips em lucro. O stop é então deslocado para `preço atual - TrailingStop` para posições compradas (ou `preço atual + TrailingStop` para posições vendidas) e só se move se o novo stop estiver pelo menos `TrailingStep` pips além do nível anterior.
   - Saídas manuais são emitidas se o preço tocar o stop ou o alvo configurados.

## Parâmetros

- **Volume** – volume de ordem em lotes.
- **Stop Loss (pips)** – distância para o stop de proteção; definir como 0 para desabilitar.
- **Take Profit (pips)** – distância para o alvo de lucro; definir como 0 para desabilitar.
- **Trailing Stop (pips)** – distância do trailing stop; definir como 0 para desabilitar o trailing.
- **Trailing Step (pips)** – avanço mínimo além do trailing stop antes que o stop se mova (deve ser positivo quando o trailing está habilitado).
- **Reverse Signals** – inverter a interpretação comprado/vendido do cruzamento do oscilador.
- **Close Opposite** – fechar uma posição existente antes de abrir uma nova operação na direção oposta.
- **Pre Smooth Period / Method** – período e tipo de média móvel usado para suavizar os dados OHLC brutos.
- **Post Smooth Period / Method** – parâmetros de média móvel para suavizar o ponto médio de Heiken Ashi.
- **Signal Period / Method** – parâmetros de média móvel para a linha de sinal do oscilador.
- **Candle Type** – fonte de velas usada para cálculos (período padrão de 15 minutos).

## Notas de implementação

- A conversão reproduz o Heiken Ashi Smoothed Oscillator original encadeando indicadores de média móvel do StockSharp (SMA, EMA, SMMA, LWMA) para pré-suavizar preços, construir a série Heiken Ashi e derivar a diferença do oscilador.
- As distâncias em pips são traduzidas para deslocamentos de preço absolutos usando o tamanho de tick e a precisão decimal do instrumento, correspondendo ao tratamento de 3/5 dígitos do MetaTrader.
- As verificações manuais de stop/alvo e o trailing stop baseado em passos são executados em cada vela terminada, refletindo de perto o comportamento da versão MQL.
- Os sinais são processados apenas quando todos os valores necessários estão disponíveis; estados parciais do indicador são ignorados até que dados suficientes tenham se acumulado.

Nenhuma tradução Python é fornecida neste diretório.
