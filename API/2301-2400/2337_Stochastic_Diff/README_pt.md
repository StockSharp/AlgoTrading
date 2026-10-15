# Estratégia de Diferença Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base na diferença entre as linhas %K e %D do oscilador Stochastic. A diferença é suavizada com uma média móvel exponencial para reduzir o ruído. Uma posição comprada é aberta quando a diferença suavizada forma um mínimo local e vira para cima. Uma posição vendida é aberta quando a diferença suavizada forma um máximo local e vira para baixo.

## Como Funciona

1. Calcular o Stochastic %K e %D com períodos definidos pelo usuário.
2. Calcular a diferença `%K - %D` e suavizá-la com uma EMA.
3. Detectar pontos de virada na diferença suavizada:
   - Se o valor estava caindo e depois sobe, abrir uma posição comprada.
   - Se o valor estava subindo e depois cai, abrir uma posição vendida.
4. Aplicar proteções opcionais de stop-loss e take-profit em percentual.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| Candle Type | Tipo de vela usado para os cálculos |
| %K Period | Período para a linha %K |
| %D Period | Período para a linha %D |
| Slowing | Suavização adicional de %K |
| Smoothing Length | Comprimento da EMA para a diferença |
| Stop Loss % | Tamanho do stop-loss em percentual |
| Take Profit % | Tamanho do take-profit em percentual |

## Notas

- Funciona em qualquer instrumento e período suportado pelo feed de dados.
- Desenvolvido para fins educacionais para demonstrar sinais de entrada baseados em indicadores.
