# Estratégia de Conceito Smart Money - Uncle Sam
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia de rompimento monitora os máximos e mínimos de swing recentes. Uma operação comprada é aberta quando o preço fecha acima do último pivô alto, enquanto uma operação vendida é aberta quando o preço fecha abaixo do último pivô baixo. Um filtro de média móvel opcional pode ser ativado para operar apenas com a tendência predominante.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: O fechamento cruza acima do pivô alto mais recente (e acima da MA se ativada).
  - **Vendido**: O fechamento cruza abaixo do pivô baixo mais recente (e abaixo da MA se ativada).
- **Comprado/Vendido**: Ambos.
- **Indicadores**: Detecção de pivô, Média Móvel (opcional).
- **Período**: Configurável.
- **Complexidade**: Moderado.
