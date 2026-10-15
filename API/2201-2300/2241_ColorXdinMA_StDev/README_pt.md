# Estratégia ColorXdinMA com Desvio Padrão
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma portagem StockSharp do especialista MQL5 **Exp_ColorXdinMA_StDev**.
Ela combina duas médias móveis em uma única linha chamada `XdinMA` e rastreia sua
variação ao longo do tempo. A diferença entre o valor atual e o anterior de `XdinMA`
é comparada com um múltiplo do seu desvio padrão recente. Quando a
variação supera o limiar positivo, uma posição comprada é aberta, enquanto uma queda
abaixo do limiar negativo abre uma posição vendida.

## Como funciona

1. Duas médias móveis simples são calculadas:
   - **Main MA** – período definido por `MainLength`.
   - **Plus MA** – período definido por `PlusLength`.
2. A linha personalizada `XdinMA = 2 * MainMA - PlusMA` é construída.
3. A variação de `XdinMA` entre velas consecutivas é passada a um indicador de desvio padrão com comprimento `StdPeriod`.
4. Se a variação for maior que `K1 * StdDev`, uma ordem de compra é colocada. Se for menor que `-K1 * StdDev`, uma ordem de venda é colocada. Posições opostas existentes são fechadas antes de abrir uma nova.

## Parâmetros

| Parâmetro   | Descrição                                          |
|-------------|----------------------------------------------------|
| `MainLength`| Período para a média móvel principal.              |
| `PlusLength`| Período para a média móvel secundária.             |
| `StdPeriod` | Número de barras usadas para o desvio padrão.      |
| `K1`        | Multiplicador para o limiar de desvio.             |
| `K2`        | Reservado para futura extensão do segundo filtro.  |

Todos os parâmetros são expostos através de `StrategyParam` para que possam ser otimizados ou
alterados a partir da interface do utilizador.

## Notas

- Apenas velas concluídas são processadas.
- A estratégia usa ordens de mercado e não implementa lógica de stop-loss ou
  take-profit.
- O gráfico inclui ambas as médias móviles e as operações executadas para análise
  visual.
