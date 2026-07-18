# Estratégia Bill Williams Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa uma versão simplificada da abordagem de trading de Bill Williams baseada no indicador **Alligator** e **Fractals**.

## Como funciona

- Calcula as linhas do Alligator usando Médias Móveis Suavizadas (SMMA):
  - Comprimento de **Jaw** (padrão 13)
  - Comprimento de **Teeth** (padrão 8)
  - Comprimento de **Lips** (padrão 5)
- Detecta fractais de alta e de baixa em velas concluídas.
- **Comprar** quando o preço rompe acima do último fractal superior que está acima da linha teeth do Alligator.
- **Vender** quando o preço rompe abaixo do último fractal inferior que está abaixo da linha teeth do Alligator.
- **Sair** de posições compradas quando o preço de fechamento cai abaixo da linha lips.
- **Sair** de posições vendidas quando o preço de fechamento sobe acima da linha lips.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | --------- | ------ |
| `JawLength` | Período da SMMA da mandíbula do Alligator | 13 |
| `TeethLength` | Período da SMMA dos dentes do Alligator | 8 |
| `LipsLength` | Período da SMMA dos lábios do Alligator | 5 |
| `CandleType` | Tipo de vela usado para cálculos | Velas de 15 minutos |

Todos os parâmetros podem ser otimizados pela interface de parâmetros da estratégia.

## Uso

1. Compilar a solução:
   ```bash
   dotnet build
   ```
2. Iniciar a estratégia no ambiente StockSharp e selecionar o instrumento e o período desejados.

## Notas

Este exemplo demonstra o uso da API de alto nível com vinculações de indicadores e não implementa dimensionamento de posições ou gestão de risco além de saídas simples.
