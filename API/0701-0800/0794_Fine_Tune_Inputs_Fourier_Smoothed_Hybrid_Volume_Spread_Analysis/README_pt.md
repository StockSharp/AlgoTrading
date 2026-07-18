# Ajuste Fino de Entradas: Análise Híbrida de Dispersão de Volume Suavizado por Fourier
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia combina volume suavizado com a EMA dos preços de abertura e fechamento para analisar a dispersão de volume. Entra comprado quando tanto a dispersão de volume quanto sua média móvel são positivas, e vendido quando ambas são negativas. Um parâmetro opcional permite fechar posições quando não há sinal.

## Detalhes

- **Condições de entrada**:
  - **Comprado**: `vd > 0` e `vdma > 0`
  - **Vendido**: `vd < 0` e `vdma < 0`
- **Condições de saída**: Fechar posição opcionalmente quando os sinais são neutros.
- **Tipo**: Seguidor de tendência
- **Indicadores**: EMA
- **Período**: 1 minuto (padrão)
