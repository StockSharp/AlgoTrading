# Estratégia Currencyprofits de Canal Alto-Baixo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia é um port em StockSharp do consultor especialista do MetaTrader `Currencyprofits_01.1`. Combina um filtro de tendência de médias móveis rápida/lenta com um rompimento do extremo do canal recente. Quando a média móvel rápida está acima da lenta, a estratégia espera um ambiente de alta e aguarda que o preço reteste a mínima mais baixa da janela do canal anterior. Operações vendidas são realizadas quando a média rápida está abaixo da lenta e o preço retesta a máxima mais alta do canal.

A implementação funciona em qualquer instrumento que forneça dados de velas. Todos os cálculos são realizados em velas fechadas para garantir estabilidade tanto em backtests quanto em operativa ao vivo.

## Lógica de trading
1. Assinar o tipo de vela configurado e calcular duas médias móveis e um canal estilo Donchian baseado nas anteriores `ChannelLength` velas (padrão: 6 barras).
2. Armazenar os valores anteriores das velas dos indicadores para imitar a lógica MQL original que usa um deslocamento de uma barra.
3. **Entrada comprada**: quando a MA rápida anterior é maior que a MA lenta anterior e a mínima da vela atual toca ou rompe a mínima do canal anterior.
4. **Entrada vendida**: quando a MA rápida anterior é menor que a MA lenta anterior e a máxima da vela atual toca ou rompe a máxima do canal anterior.
5. **Regras de saída**:
   - Fechar posições compradas se a próxima vela fechar acima da máxima do canal armazenado ou se o stop protetor for atingido.
   - Fechar posições vendidas se a próxima vela fechar abaixo da mínima do canal armazenado ou se o stop-loss for acionado.
6. Apenas uma posição está ativa por vez; a estratégia ignora novos sinais enquanto uma operação estiver aberta.

## Dimensionamento de posição
- `RiskPercent` define a fração do valor do portfólio que pode ser arriscada por operação (padrão `0.14`, ou seja, 14%).
- A distância do stop-loss é derivada de `StopLossPoints` multiplicada pelo `PriceStep` do instrumento (ou pontos se não houver metadados disponíveis).
- O risco em dinheiro por contrato é estimado com o valor de passo de câmbio (`StepPrice`). Se o instrumento não expõe essa informação, a distância de preço bruta é usada.
- O volume final da ordem é alinhado às restrições de trading do instrumento (`VolumeStep`, `MinVolume`, `MaxVolume`). Se o dimensionamento baseado em risco não puder ser calculado, o `Volume` base da estratégia é usado.

## Parâmetros
- `FastLength` – comprimento da média móvel rápida usada para detectar a tendência (padrão 32).
- `FastMaType` – tipo da média móvel rápida (Simple, Exponential, Smoothed, Weighted).
- `SlowLength` – comprimento da média móvel lenta (padrão 86).
- `SlowMaType` – tipo da média móvel lenta.
- `PriceSource` – preço de vela aplicado a ambas as médias móveis (padrão Close).
- `ChannelLength` – número de velas anteriores que formam o canal alto/baixo (padrão 6).
- `StopLossPoints` – distância do stop expressa em pontos do instrumento antes de ser convertida em preço (padrão 170).
- `RiskPercent` – fração do capital arriscado por operação (padrão 0.14 → 14%).
- `CandleType` – período das velas usadas para todos os cálculos (padrão 1 hora, pode ser alterado para corresponder ao período do gráfico desejado).

## Notas de uso
- Garantir que `Security.PriceStep`, `Security.StepPrice` e os metadados de volume estejam preenchidos para um dimensionamento preciso da posição.
- Definir o `Volume` da estratégia para um valor de fallback razoável quando o dimensionamento baseado em risco estiver desabilitado (p. ex., `RiskPercent = 0`).
- A lógica opera em velas fechadas; as execuções ao vivo ocorrem no fechamento da barra que confirma o sinal.
- O stop-loss é gerenciado internamente; não há take-profit separado, refletindo o consultor especialista fonte.

## Fonte
Convertido de `MQL/17641/Currencyprofits_01.1.mq5` com ênfase em legibilidade e compatibilidade com a API de alto nível do StockSharp.
