# Estratégia CHO With Flat
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no cruzamento do **Chaikin Oscillator** com sua média móvel. Um filtro de Bandas de Bollinger é usado para evitar operações em mercados laterais.

## Parâmetros
- **Candle Type** – período das velas de entrada.
- **Fast Period** – período rápido do Chaikin Oscillator.
- **Slow Period** – período lento do Chaikin Oscillator.
- **MA Period** – período da média móvel aplicada ao oscilador.
- **MA Type** – tipo de média móvel para a linha de sinal.
- **Bollinger Period** – período das Bandas de Bollinger.
- **Std Deviation** – desvio padrão para as Bandas de Bollinger.
- **Flat Threshold** – largura mínima de banda (em pontos) para considerar o mercado ativo.

## Lógica de negociação
1. Calcular o Chaikin Oscillator e sua média móvel.
2. Construir Bandas de Bollinger no preço para detecção de mercado lateral.
3. Ignorar operações se a largura da banda de Bollinger estiver abaixo de `Flat Threshold`.
4. **Comprar** quando o oscilador cruza abaixo de sua linha de sinal.
5. **Vender** quando o oscilador cruza acima de sua linha de sinal.

A direção da posição sempre segue o cruzamento mais recente, enquanto o filtro lateral impede a negociação em condições de mercado lateral.
