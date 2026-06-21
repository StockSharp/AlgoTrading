# Estratégia de Cruzamento de Vela AML
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera com base no indicador Adaptive Market Level (AML).
Uma operação é aberta quando o valor do AML está dentro do corpo da vela atual:
se a vela fecha acima da abertura e o AML está entre eles, uma posição comprada
é aberta. Para velas de baixa, a condição oposta abre uma posição vendida. Opcionalmente,
a posição pode ser revertida quando o sinal oposto aparecer.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: vela de alta e `open <= AML <= close`.
  - **Vendido**: vela de baixa e `open >= AML >= close`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Posição revertida no sinal oposto quando habilitado.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Fractal` = 70
  - `Lag` = 18
  - `Shift` = 0
  - `UseOpposite` = true
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Único (AML)
  - Stops: Não
  - Complexidade: Médio
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
