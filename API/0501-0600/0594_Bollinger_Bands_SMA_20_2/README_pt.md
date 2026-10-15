# Estratégia de Bollinger Bands SMA 20-2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia usa Bollinger Bands construídas a partir de uma média móvel simples de 20 períodos com um multiplicador de 2 desvios padrão. Fica comprado quando o preço cruza acima da banda inferior e vendido quando o preço cruza abaixo da banda superior. As posições se revertem em sinais opostos sem stop losses explícitos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Close` cruza acima da banda inferior.
  - **Vendido**: `Close` cruza abaixo da banda superior.
- **Comprado/Vendido**: Ambas as direções.
- **Critérios de saída**:
  - Sinal oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Bollinger Length` = 20
  - `Bollinger Multiplier` = 2
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: Único
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Baixo
