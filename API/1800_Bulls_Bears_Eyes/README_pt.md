# Estratégia Bulls Bears Eyes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia avalia o equilíbrio entre a pressão de alta e de baixa usando os indicadores **Bulls Power** e **Bears Power**. Os dois indicadores são combinados em um único oscilador escalado de 0 a 100. Valores altos indicam dominância dos compradores, enquanto valores baixos apontam para a força dos vendedores.

As decisões de negociação são baseadas em níveis de limiar semelhantes ao especialista original *BullsBearsEyes*. Quando o oscilador cruza acima do nível de sobrecompra após estar abaixo dele, uma posição comprada é aberta e qualquer posição vendida é fechada. Por outro lado, cruzar abaixo do nível de sobrevenda aciona uma entrada vendida e fecha as posições compradas existentes. Valores neutros entre os limiares mantêm a posição atual, mas fecham operações opostas.

## Parâmetros
- **Period** – período de média para Bulls/Bears Power (padrão: 13).
- **High Level** – limiar de sobrecompra que gera sinais de compra (padrão: 75).
- **Middle Level** – nível médio de referência usado para interpretação de tendência (padrão: 50).
- **Low Level** – limiar de sobrevenda que gera sinais de venda (padrão: 25).
- **Candle Type** – período das velas processadas pela estratégia (padrão: velas de 4 horas).

## Regras de entrada e saída
1. Calcular Bulls Power e Bears Power para cada vela e derivar o valor do oscilador entre 0 e 100.
2. **Entrada comprada**: o oscilador cruza acima de *High Level* após estar abaixo. Qualquer posição vendida é fechada antes de abrir a comprada.
3. **Entrada vendida**: o oscilador cruza abaixo de *Low Level* após estar acima. Qualquer posição comprada existente é fechada antes de abrir a vendida.
4. **Saída de posição**: quando o oscilador muda de lado (acima/abaixo da zona central), a posição oposta é fechada.

O oscilador também é plotado junto com as velas para análise visual.

## Observações
- A estratégia usa a API de alto nível `SubscribeCandles` e `Bind` para processamento de indicadores.
- Mecanismos de proteção são ativados via `StartProtection()` no início.
- Apenas velas completadas são avaliadas para evitar sinais prematuros.
