# Estratégia Fisher Cyber Cycle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia aplica a Transformada de Fisher ao indicador Cyber Cycle de Ehlers. Uma posição comprada é aberta quando a linha Fisher cruza acima de sua linha de gatilho, enquanto uma posição vendida é aberta em um cruzamento descendente. As posições são fechadas ou revertidas no cruzamento oposto.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: `Fisher > Trigger` && `Fisher anterior <= Trigger anterior`
  - **Vendido**: `Fisher < Trigger` && `Fisher anterior >= Trigger anterior`
- **Critérios de saída**:
  - Cruzamento oposto de Fisher e Trigger
- **Stops**: Nenhum
- **Valores padrão**:
  - `Alpha` = 0.07
  - `Length` = 8
  - `Candle Type` = período de 8 horas
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: Fisher Transform, Cyber Cycle
  - Stops: Nenhum
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
