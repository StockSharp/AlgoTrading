# Estratégia Exp BlauCMI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A estratégia recria o assessor especialista do MetaTrader 5 **Exp_BlauCMI** usando a API de alto nível do StockSharp. Ela calcula o Blau Candle Momentum Index (CMI), uma razão de momentum com triplo suavizado, em uma série de candles configurável e reage às oscilações do oscilador. Operações compradas são abertas quando o indicador vira para cima após uma queda, as vendidas quando vira para baixo após uma alta. O módulo mantém a implementação totalmente orientada a eventos — as ordens são enviadas apenas após o fechamento dos candles.

## Lógica do indicador
1. Duas fontes de preço são selecionadas através de `Momentum Price` e `Reference Price`. O momentum bruto é a diferença entre o valor atual do primeiro preço e o valor atrasado do segundo preço. O atraso é controlado por `Momentum Depth`.
2. Tanto o momentum quanto seu valor absoluto passam por três médias móveis consecutivas (`First/Second/Third Smoothing`). O mesmo método de média é usado para cada estágio e pode ser selecionado entre médias móveis simples, exponenciais, suavizadas (RMA) e ponderadas linearmente.
3. O Blau CMI é calculado como `100 * smoothedMomentum / smoothedAbsMomentum`. O indicador começa a produzir sinais de trading assim que o terceiro estágio de suavizado acumulou barras suficientes.
4. O parâmetro `Signal Shift` determina quantos candles fechados para trás a estratégia inspeciona antes de avaliar reversões (um valor de 1 reproduz o EA original e usa a última barra fechada).

## Regras de trading
- **Entrada comprada** – permitida quando `Allow Long Entry` está habilitado e a sequência de indicador `Value[Signal Shift - 1] < Value[Signal Shift - 2]` seguida de `Value[Signal Shift] > Value[Signal Shift - 1]` é observada, significando que o oscilador acabou de virar para cima. Posições vendidas existentes são fechadas primeiro se `Allow Short Exit` estiver habilitado.
- **Entrada vendida** – permitida quando `Allow Short Entry` está habilitado e o indicador vira para baixo (`Value[Signal Shift - 1] > Value[Signal Shift - 2]` e `Value[Signal Shift] < Value[Signal Shift - 1]`). Posições compradas existentes são fechadas de antemão se `Allow Long Exit` estiver habilitado.
- **Saída comprada** – quando em uma posição comprada e a condição de entrada vendida dispara, a posição é fechada se `Allow Long Exit` for verdadeiro.
- **Saída vendida** – quando em uma posição vendida e a condição de entrada comprada dispara, a posição é fechada se `Allow Short Exit` for verdadeiro.
- Todas as negociações são executadas com ordens de mercado usando o volume especificado em `Order Volume`. Brackets de stop-loss e take-profit protetores são anexados automaticamente via `StartProtection` e permanecem ativos enquanto a posição estiver aberta.

## Parâmetros
- `Candle Type` – tipo de dado (período ou outra descrição de candles) usado para cálculo do indicador e decisões de trading. O padrão são candles de 4 horas.
- `Smoothing Method` – algoritmo de média compartilhado pelos três estágios de suavizado (Simples, Exponencial, Suavizado, Ponderado Linear).
- `Momentum Depth` – número de barras entre os dois pontos de preço que formam o momentum bruto.
- `First/Second/Third Smoothing` – comprimentos dos três estágios de média aplicados tanto ao momentum quanto ao seu valor absoluto.
- `Signal Shift` – número de candles já fechados a serem inspecionados ao avaliar padrões de reversão (valor mínimo é 1).
- `Momentum Price` – preço aplicado usado para o lado não atrasado do cálculo do momentum.
- `Reference Price` – preço aplicado usado para o lado de comparação atrasado.
- `Allow Long Entry`, `Allow Short Entry` – interruptores para permitir a abertura de negociações em cada direção.
- `Allow Long Exit`, `Allow Short Exit` – interruptores que controlam se os sinais opostos fecham as respectivas posições.
- `Stop-Loss Points`, `Take-Profit Points` – limites de risco medidos em passos de preço (`Security.PriceStep`). Quando definidos como zero, o bracket correspondente é desabilitado.
- `Order Volume` – quantidade absoluta usada ao enviar ordens de mercado. A estratégia também atribui esse valor à propriedade base `Strategy.Volume`.

## Notas adicionais
- Os métodos de suavizado suportados correspondem a indicadores do StockSharp: Média Móvel Simples, Média Móvel Exponencial, Média Móvel Suavizada (RMA) e Média Móvel Ponderada.
- A constante de preço Demark replica a implementação do MT5 calculando a média dos extremos de preço e do fechamento do candle antes de ajustar as distâncias alta/baixa.
- Como os cálculos usam apenas candles finalizados, a estratégia reage uma vez por barra, correspondendo ao comportamento original do EA que verificava novas barras via `IsNewBar`.
- `Stop-Loss Points` e `Take-Profit Points` são interpretados como múltiplos do passo de preço do instrumento para manter consistência com as entradas baseadas em pontos da estratégia MQL5 original.
