# Estratégia Fisher Org Sign
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o indicador Fisher Transform com níveis superior e inferior predefinidos. Uma posição comprada é aberta quando o valor Fisher cruza acima do nível inferior. Uma posição vendida é aberta quando o valor Fisher cruza abaixo do nível superior.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Fisher crosses above DownLevel`
  - **Vendido**: `Fisher crosses below UpLevel`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Sinal oposto aciona a reversão da posição
- **Stops**: Não
- **Valores padrão**:
  - `Fisher Length` = 7
  - `UpLevel` = 1.5
  - `DownLevel` = -1.5
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Fisher Transform
  - Stops: Não
  - Complexidade: Baixo
  - Período: Médio prazo (H4)
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
