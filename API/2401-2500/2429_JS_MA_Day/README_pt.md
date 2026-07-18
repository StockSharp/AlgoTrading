# Estratégia JS MA Day
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia JS MA Day** opera com base em uma média móvel simples calculada em velas diárias usando o preço mediano. A estratégia compara a posição da média móvel em relação ao preço de abertura de cada dia e abre posições quando a tendência da média móvel confirma um cruzamento do preço de abertura.

## Indicadores

- Média Móvel Simples (preço mediano)

## Parâmetros

| Nome | Descrição | Padrão |
|------|-----------|--------|
| `MaPeriod` | Período da média móvel simples. | `3` |
| `Reverse` | Inverte os sinais de negociação. Quando habilitado, os sinais de compra tornam-se sinais de venda e vice-versa. | `false` |
| `CandleType` | Tipo de vela usado para cálculos. O padrão são velas de período diário. | `TimeFrame(1 day)` |

## Regras de entrada

1. Avalia a média móvel simples (SMA) diária e os preços de abertura diários.
2. **Comprar** quando:
   - A SMA atual está abaixo da SMA anterior.
   - A SMA atual está acima do preço de abertura de hoje.
   - A SMA anterior está abaixo da SMA de dois dias atrás.
   - A SMA anterior está acima do preço de abertura do dia anterior.
3. **Vender** quando:
   - A SMA atual está acima da SMA anterior.
   - A SMA atual está abaixo do preço de abertura de hoje.
   - A SMA anterior está acima da SMA de dois dias atrás.
   - A SMA anterior está abaixo do preço de abertura do dia anterior.
4. Se `Reverse` estiver habilitado, as condições de compra e venda são trocadas.

## Regras de saída

- As posições são fechadas chamando `StartProtection`, que permite configurar ordens de proteção como stop loss ou take profit através das configurações da plataforma.

## Notas

- A estratégia processa apenas velas completadas.
- O volume das ordens é definido pela propriedade `Volume` da classe base.
- Ainda não existe uma versão em Python desta estratégia.
