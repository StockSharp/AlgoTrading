# Estratégia de Correlação com Índice de Altcoins
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia compara as tendências de EMA do instrumento negociado e de um índice de referência. Abre comprado quando ambas as EMAs rápidas estão acima de suas EMAs lentas, e vendido quando ambas estão abaixo. A lógica inversa opcional permite operar contra a tendência do índice ou ignorá-lo completamente.

## Detalhes

- **Critérios de entrada**:
  - EMA rápida acima da EMA lenta em ambos os instrumentos (ou o oposto se inverso).
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**:
  - Condição de cruzamento oposto.
- **Stops**: Nenhum.
- **Valores padrão**:
  - `FastEmaLength` = 47
  - `SlowEmaLength` = 50
  - `IndexFastEmaLength` = 47
  - `IndexSlowEmaLength` = 50
  - `SkipIndexReference` = false
  - `InverseSignal` = false
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: EMA
  - Stops: Não
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
