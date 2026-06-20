# Estratégia de MACD Long
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Combina os extremos do Relative Strength Index com os cruzamentos do MACD para capturar recuos dentro de uma tendência. Após o RSI atingir uma leitura extrema, o sistema aguarda um cruzamento confirmatório do MACD antes de entrar. Esta abordagem filtra mudanças de momentum ruidosas e foca em reversões de alta probabilidade.

A estratégia opera em ambas as direções e pode mudar rapidamente quando sinais opostos aparecem. MACD fornece confirmação de momentum enquanto RSI destaca zonas de sobrecompra e sobrevenda. Stops protetores podem ser adicionados por meio dos controles de risco do motor.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: RSI cai abaixo da sobrevenda, então a linha MACD cruza acima do sinal.
  - **Vendido**: RSI sobe acima da sobrecompra, então a linha MACD cruza abaixo do sinal.
- **Critérios de saída**:
  - Cruzamento oposto ou stop acionado.
- **Indicadores**:
  - RSI (comprimento 14, sobrevenda 30, sobrecompra 70)
  - MACD (rápido 12, lento 26, sinal 9)
- **Stops**: Implementar via StartProtection ou gestão de capital externa.
- **Valores padrão**:
  - `RsiLength` = 14
  - `Oversold` = 30
  - `Overbought` = 70
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
- **Filtros**:
  - Reversão de momentum
  - Funciona em vários períodos
  - Indicadores: RSI, MACD
  - Stops: Opcional
  - Complexidade: Básico
