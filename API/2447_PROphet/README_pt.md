# Estratégia PROphet
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia PROphet avalia os intervalos de preço das últimas três velas completadas para gerar sinais durante horas de trading especificadas. Uma função personalizada combina os intervalos com coeficientes definidos pelo usuário. Se a função for positiva, a estratégia abre uma posição na direção correspondente.

Os trades comprados usam os coeficientes `X1..X4` e um trailing stop definido por `BuyStopPoints`. Os trades vendidos usam os coeficientes `Y1..Y4` e `SellStopPoints`. Os stops seguem o preço quando ele se move a favor da posição em mais do spread mais o dobro da distância do stop. As posições são fechadas após as 18:00 ou quando o trailing stop é atingido.

## Detalhes

- **Critérios de entrada**
  - **Comprado**: `Qu(X1,X2,X3,X4) > 0` e hora atual entre 10 e 18.
  - **Vendido**: `Qu(Y1,Y2,Y3,Y4) > 0` e hora atual entre 10 e 18.
- **Critérios de saída**
  - **Comprado**: Hora > 18 ou o melhor preço de compra cai abaixo do trailing stop.
  - **Vendido**: Hora > 18 ou o melhor preço de venda sobe acima do trailing stop.
- **Parâmetros**
  - `EnableBuy` – permitir abertura de posições compradas.
  - `EnableSell` – permitir abertura de posições vendidas.
  - `X1, X2, X3, X4` – coeficientes para a função de sinal comprado.
  - `Y1, Y2, Y3, Y4` – coeficientes para a função de sinal vendido.
  - `BuyStopPoints` – distância do trailing stop em pontos para trades comprados.
  - `SellStopPoints` – distância do trailing stop em pontos para trades vendidos.
  - `CandleType` – tipo de vela para cálculos (padrão 5 minutos).
- **Filtros**
  - Categoria: Intradiário
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Trailing
  - Complexidade: Moderado
  - Período: Curto prazo
