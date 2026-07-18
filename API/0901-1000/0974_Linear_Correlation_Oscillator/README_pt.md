# Estratégia do Oscilador de Correlação Linear
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia do Oscilador de Correlação Linear mede a correlação entre preço e tempo em uma janela deslizante. A estratégia vai comprado quando o oscilador cruza acima de zero e vai vendido quando cruza abaixo de zero.

## Detalhes

- **Critérios de entrada**:
  - Oscilador cruza acima de zero → **Comprado**.
  - Oscilador cruza abaixo de zero → **Vendido**.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Cruzamento de zero oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `Length` = 14
- **Filtros**:
  - Categoria: Oscilador
  - Direção: Ambos
  - Indicadores: Linear Correlation
  - Stops: Nenhum
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
