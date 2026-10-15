# Estratégia MACD com Redes Neurais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina um filtro de perceptron simples de quatro pesos com um cruzamento clássico de MACD. Uma posição é aberta apenas quando tanto o MACD quanto a rede neural concordam na direção.

## Como funciona

1. **Filtro de perceptron**  
   Três perceptrons avaliam o momentum do preço usando as diferenças entre o fechamento atual e uma série de preços de abertura passados. Cada perceptron tem quatro pesos inteiros (`X11`…`X34`) onde `0` significa sem influência. A saída do perceptron é uma soma ponderada das diferenças de preço.  
   Dependendo do parâmetro `Pass`, um, dois ou todos os três perceptrons participam da tomada de decisão. O filtro também define distâncias de stop-loss e take-profit (`Sl1`, `Tp1`, `Sl2`, `Tp2`).
2. **Confirmação MACD**  
   Um MACD padrão (12, 26, 9) é calculado. Um sinal de compra aparece quando a linha MACD está abaixo de zero e cruza acima da linha de sinal. Um sinal de venda ocorre quando a linha está acima de zero e cruza abaixo da linha de sinal.
3. **Execução de operações**  
   - Uma posição comprada é aberta se tanto o MACD quanto o filtro de perceptron são positivos.  
   - Uma posição vendida é aberta se ambos são negativos.  
   A posição é fechada quando um nível de stop-loss ou take-profit é atingido.

## Parâmetros

| Nome | Descrição |
| ---- | --------- |
| `X11…X34` | Pesos para entradas do perceptron. |
| `Tp1`, `Sl1` | Take-profit e stop-loss para o primeiro perceptron. |
| `Tp2`, `Sl2` | Take-profit e stop-loss para o segundo perceptron. |
| `P1`, `P2`, `P3` | Deslocamentos em barras usados para calcular as entradas do perceptron. |
| `Pass` | Número de perceptrons a usar (1-3). |
| `CandleType` | Série de velas para cálculos. |

