# Estratégia de MA Cross + DMI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera um cruzamento de médias móveis exponenciais rápida e lenta apenas quando o Directional Movement Index confirma a força da tendência. Ao aguardar que o +DI ou -DI domine enquanto o ADX sobe acima de um nível chave, o sistema filtra cruzamentos fracos.

Esta estratégia pode entrar em posições compradas ou vendidas e sai em cruzamentos opostos. A filtragem pelo ADX ajuda o método a ficar fora de períodos de consolidação onde as médias móveis frequentemente geram sinais falsos.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: EMA rápida cruza acima da EMA lenta, +DI > -DI e ADX acima do nível chave.
  - **Vendido**: EMA rápida cruza abaixo da EMA lenta, -DI > +DI e ADX acima do nível chave.
- **Critérios de saída**:
  - Cruzamento oposto ou stop manual.
- **Indicadores**:
  - Duas EMAs (períodos 10 e 20)
  - Directional Movement Index (comprimento 14, suavização ADX 14)
- **Stops**: Nenhum por padrão; pode usar StartProtection.
- **Valores padrão**:
  - `Ma1Length` = 10
  - `Ma2Length` = 20
  - `DmiLength` = 14
  - `AdxSmoothing` = 14
  - `KeyLevel` = 20
- **Filtros**:
  - Seguidor de tendência
  - Funciona em períodos intradiário a swing
  - Indicadores: EMA, DMI
  - Stops: Opcional
  - Complexidade: Básico
