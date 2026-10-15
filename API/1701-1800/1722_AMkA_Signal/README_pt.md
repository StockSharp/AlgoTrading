# Estratégia AMkA Signal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia usa a derivada da Média Móvel Adaptativa de Kaufman (KAMA) combinada com um filtro de volatilidade baseado no desvio padrão. Uma posição comprada é aberta quando a taxa de variação do KAMA supera um limiar dinâmico; uma posição vendida é aberta quando cai abaixo do limiar negativo. O limiar é calculado multiplicando o desvio padrão das variações do KAMA por um fator definido pelo usuário.

## Parâmetros

- **KAMA Length** – período de retrospecto para o indicador KAMA.
- **Fast Period** – período rápido de EMA usado no suavizamento do KAMA.
- **Slow Period** – período lento de EMA usado no suavizamento do KAMA.
- **Deviation Multiplier** – multiplicador aplicado ao desvio padrão para formar o limiar do sinal.
- **Take Profit** – percentual para fixação automática de lucros.
- **Stop Loss** – percentual para o stop de proteção.
- **Candle Type** – período das velas utilizadas para os cálculos.

## Lógica de negociação

1. Assinar velas do período selecionado.
2. Calcular o KAMA para cada vela e computar sua variação em relação ao valor anterior.
3. Atualizar o indicador de desvio padrão com os valores de variação.
4. Quando a variação excede `Deviation Multiplier * StdDev`, abrir ou fechar posições:
   - Se a variação for maior que o limiar: fechar posições vendidas e abrir comprada.
   - Se a variação for menor que o limiar negativo: fechar posições compradas e abrir vendida.
5. Ordens de proteção para take profit e stop loss são gerenciadas automaticamente via `StartProtection`.

## Notas

A estratégia trabalha apenas com velas completadas e usa tabulações para recuo no código-fonte. Todos os comentários são escritos em inglês conforme exigido.
