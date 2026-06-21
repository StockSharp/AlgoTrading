# Estratégia de Price Radio
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia implementa o indicador Price Radio de John Ehlers. Entra comprado quando a derivada do preço supera os limiares de amplitude e frequência, e entra vendido quando cai abaixo de seus valores negativos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: derivada maior que amplitude e frequência.
  - **Vendido**: derivada menor que amplitude negativa e frequência negativa.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `Length` = 14.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Custom
  - Stops: Não
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
