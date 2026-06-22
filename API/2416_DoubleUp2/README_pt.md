# Estratégia DoubleUp2 CCI MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

DoubleUp2 é uma estratégia estilo martingale que combina o Commodity Channel Index (CCI) e o MACD.
Abre posições vendidas quando ambos os indicadores mostram valores positivos extremos e posições compradas quando ambos são extremamente negativos.
Após uma operação perdedora, o tamanho da posição dobra, buscando recuperar as perdas anteriores.
Operações lucrativas são fechadas quando o preço avança um número fixo de pontos.

## Detalhes

- **Critérios de Entrada**:
  - **Comprado**: `CCI < -Threshold` e `MACD < -Threshold`.
  - **Vendido**: `CCI > Threshold` e `MACD > Threshold`.
- **Comprado/Vendido**: Ambos.
- **Critérios de Saída**:
  - Sinal oposto ou o preço se move `ExitDistance` pontos em lucro.
- **Stops**: Sem stop-loss explícito.
- **Valores padrão**:
  - `CCI Period` = 8
  - `MACD Fast` = 13
  - `MACD Slow` = 33
  - `MACD Signal` = 2
  - `Threshold` = 230
  - `Base Volume` = 0.1
  - `ExitDistance` = `120 * price step`
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: CCI, MACD
  - Stops: Não
  - Complexidade: Moderado
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Alto
