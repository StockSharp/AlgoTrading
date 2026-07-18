# Estratégia de Velas Suavizadas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base na cor das velas suavizadas. Para cada vela concluída, a diferença entre o preço de fechamento e abertura é passada por uma média móvel. Quando essa diferença suavizada muda de sinal, a "cor" da vela muda e a estratégia inverte sua posição.

## Lógica

1. Inscrever-se em uma série de velas configurável.
2. Calcular `diff = close - open` para cada vela concluída.
3. Suavizar o `diff` usando a média móvel selecionada.
4. Determinar a cor da vela:
   - **Cor 0** se `smoothed diff > 0` (fechamento acima da abertura).
   - **Cor 1** caso contrário.
5. Gerar sinais:
   - **Comprar** quando a cor muda de 0 para 1.
   - **Vender** quando a cor muda de 1 para 0.
6. A posição atual é fechada antes de abrir uma nova.

## Parâmetros

- `CandleType` – período das velas processadas. Padrão é 1 hora.
- `MaLength` – comprimento da média móvel de suavização. Padrão é 30.
- `MaMethods` – algoritmo de média móvel: `Simple`, `Exponential`, `Smma` ou `Weighted`. Padrão é `Weighted`.

## Notas

- A estratégia usa ordens a mercado via `BuyMarket` e `SellMarket`.
- A API de alto nível é usada para assinatura de velas e visualização de gráficos.
- Os valores do indicador são acessados via `TryGetValue` para evitar chamadas diretas ao buffer.
