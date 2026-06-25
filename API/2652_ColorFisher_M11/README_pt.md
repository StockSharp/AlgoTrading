# Estratégia Color Fisher M11
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Color Fisher M11 é uma estratégia seguidora de tendência que replica o consultor especializado Exp_ColorFisher_m11 do MetaTrader 5. Usa uma variante personalizada do Fisher Transform que pinta candles com cinco estados de cor para destacar momentum extremo de alta e de baixa. Os sinais são atrasados por um número configurável de candles fechados para evitar negociar com dados incompletos, enquanto interruptores opcionais permitem desabilitar entradas ou saídas para cada lado de forma independente.

## Lógica do indicador
A estratégia constrói o indicador Color Fisher em tempo real:

- Determina a máxima mais alta e a mínima mais baixa sobre a janela **Range Periods**.
- Normaliza o preço médio do candle atual dentro desse intervalo e aplica **Price Smoothing** (estilo EMA) para estabilizar as oscilações.
- Aplica o Fisher Transform com um fator adicional **Index Smoothing** para criar o valor final do oscilador.
- Classifica o oscilador em cinco bandas de cor discretas usando os limites **High Level** e **Low Level**:
  - `0` – forte impulso de alta acima do nível alto.
  - `1` – momentum de alta moderado entre zero e o nível alto.
  - `2` – zona neutra em torno de zero.
  - `3` – momentum de baixa moderado entre zero e o nível baixo.
  - `4` – forte impulso de baixa abaixo do nível baixo.

O sinal é avaliado `Signal Bar` candles atrás, imitando o comportamento do Consultor Especializado original. O estado de cor anterior também é rastreado para detectar novas transições para as bandas extremas.

## Regras de negociação
- **Entrada comprada** – permitida quando `Enable Buy Entry` é verdadeiro, a cor atrasada é igual a `0` (forte alta) e a cor anterior é diferente de `0`. Qualquer exposição vendida é revertida e a posição torna-se comprada.
- **Entrada vendida** – permitida quando `Enable Sell Entry` é verdadeiro, a cor atrasada é igual a `4` (forte baixa) e a cor anterior é diferente de `4`. Qualquer exposição comprada é revertida e a posição torna-se vendida.
- **Saída comprada** – ativada quando `Enable Buy Exit` é verdadeiro e a cor atrasada passa para `3` ou `4`, sinalizando controle de baixa.
- **Saída vendida** – ativada quando `Enable Sell Exit` é verdadeiro e a cor atrasada passa para `0` ou `1`, sinalizando controle de alta.

Para evitar múltiplas ordens por sinal, a estratégia lembra o tempo de fechamento da próxima barra para cada direção e recusa novas entradas até que o próximo candle seja concluído.

## Gestão de risco
`Stop Loss (pts)` e `Take Profit (pts)` convertem as distâncias originais em pips em passos de preço absolutos usando o passo de preço do instrumento. Quando uma distância positiva é fornecida, ordens protetoras são ativadas através de `StartProtection`. Defina qualquer valor como zero para desativar essa proteção.

## Parâmetros
- **Range Periods** – comprimento do lookback para o intervalo alto/baixo usado pelo Fisher Transform (padrão 10).
- **Price Smoothing** – fator de suavização pré-transformação, 0…0.99 (padrão 0.3).
- **Index Smoothing** – fator de suavização pós-transformação, 0…0.99 (padrão 0.3).
- **High Level / Low Level** – limites que definem extremos de alta e de baixa (padrão +1.01 e –1.01).
- **Signal Bar** – número de candles fechados para atrasar a avaliação do sinal (padrão 1).
- **Enable Buy Entry / Enable Sell Entry** – interruptores para abrir novas negociações compradas ou vendidas.
- **Enable Buy Exit / Enable Sell Exit** – interruptores para permitir saídas impulsionadas pelo indicador.
- **Stop Loss (pts) / Take Profit (pts)** – distâncias protetoras expressas em passos de preço.
- **Candle Type** – período para a assinatura de candles; padrão: candles de 4 horas.

## Notas
- A estratégia usa bindings de alto nível do StockSharp (`SubscribeCandles().BindEx`) e não armazena coleções históricas além do histórico mínimo de cores necessário para o sinal atrasado.
- Nesta versão não há port Python, de acordo com a especificação.
- Adicione a estratégia a uma área do gráfico para visualizar tanto o preço quanto o oscilador Color Fisher calculado.
